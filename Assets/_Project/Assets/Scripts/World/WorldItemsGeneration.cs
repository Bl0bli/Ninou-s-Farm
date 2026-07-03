using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class WorldItemsGeneration : MonoBehaviour
{
    [SerializeField] private Tilemap _tilemap;
    [SerializeField] private List<CommonItem> _basicCommonItems;
    [SerializeField] private List<BiomeCommonItem> _basicBiomeTiles;
    [SerializeField] private List<GameObject> _basicCommonInteractiveItems;
    [SerializeField] private List<BiomeInteractiveItem> _biomeInteractiveItems;
    [SerializeField] private Transform _interactiveItemsParent;

    [SerializeField] private float _scale = 0.1f;
    [SerializeField] private float _gridItemSpawnRate;
    [SerializeField] private float _gridItemBiomeSpawnRate;
    [Range(0f, 1f)]
    [SerializeField] private float _interactiveItemSpawnRate = 0.1f;

    private BasicItemData[,] _gridBasicItems;
    private bool[,] _occupied;
    private int _gridSize;
    private int _gridItemSize;
    
    private Dictionary<BiomeType, List<CommonItem>> _biomeCommonItemsDict;
    private Dictionary<BiomeType, List<InteractiveItem>> _biomeInteractiveItemsDict;

    /// <summary>
    /// Initialise la génération des objets et éléments de décor dans le monde.
    /// </summary>
    public IEnumerator InitRoutine()
    {
        _biomeCommonItemsDict = new Dictionary<BiomeType, List<CommonItem>>();
        foreach (var biomeItems in _basicBiomeTiles)
        {
            _biomeCommonItemsDict[biomeItems.Biome] = biomeItems.ItemTiles;
        }

        _biomeInteractiveItemsDict = new Dictionary<BiomeType, List<InteractiveItem>>();
        foreach (var entry in _biomeInteractiveItems)
        {
            _biomeInteractiveItemsDict[entry.Biome] = entry.Items;
        }

        float xoffset = Random.Range(-1000, 1000);
        float yOffset = Random.Range(-1000, 1000);

        _gridSize = GridWorld.Instance.GridSize;
        _gridItemSize = _gridSize * 3;
        _gridBasicItems = new BasicItemData[_gridItemSize, _gridItemSize];
        _occupied = new bool[_gridItemSize, _gridItemSize];
        float[,] noiseMap = new float[_gridItemSize, _gridItemSize];
        for (int y = 0; y < _gridItemSize; y++)
        {
            for (int x = 0; x < _gridItemSize; x++)
            {
                noiseMap[x, y] = Mathf.PerlinNoise(xoffset + x * _scale, yOffset + y * _scale);
            }
            if (!GridWorld.Instance.SkipGeneration && !(UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.isPressed) && y % 10 == 0) yield return null;
        }

        yield return StartCoroutine(GenerateTileMapRoutine(noiseMap));    
	}

    public void Clear()
    {
        _tilemap.ClearAllTiles();
        
        if(_interactiveItemsParent != null)
        {
            for (int i = _interactiveItemsParent.childCount - 1; i >= 0; i--)
                Destroy(_interactiveItemsParent.GetChild(i).gameObject);
        }
    }

    private IEnumerator GenerateTileMapRoutine(float[,] noiseMap)
    {
        int chunkSize = 32;

        for (int chunkX = 0; chunkX < _gridSize; chunkX += chunkSize)
        {
            for (int chunkY = 0; chunkY < _gridSize; chunkY += chunkSize)
            {
                bool skip = GridWorld.Instance.SkipGeneration || (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.isPressed);

                List<Vector3Int> tilePositions = new List<Vector3Int>();
                List<TileBase> tiles = new List<TileBase>();

                int endX = Mathf.Min(chunkX + chunkSize, _gridSize);
                int endY = Mathf.Min(chunkY + chunkSize, _gridSize);

                for (int worldX = chunkX; worldX < endX; worldX++)
                {
                    for (int worldY = chunkY; worldY < endY; worldY++)
                    {
                        TileData currentTile = GridWorld.Instance.GetTileAt(new Vector2Int(worldX, worldY));
                        
                        if (currentTile.Type == TileType.WATER) continue;

                        bool diffLeft = worldX == 0 || IsDifferent(currentTile, worldX - 1, worldY);
                        bool diffRight = worldX == _gridSize - 1 || IsDifferent(currentTile, worldX + 1, worldY);
                        bool diffBottom = worldY == 0 || IsDifferent(currentTile, worldX, worldY - 1);
                        bool diffTop = worldY == _gridSize - 1 || IsDifferent(currentTile, worldX, worldY + 1);
                        
                        for (int subY = 0; subY < 3; subY++)
                        {
                            for (int subX = 0; subX < 3; subX++)
                            {
                                if (diffLeft && subX <= 1) continue;
                                if (diffRight && subX >= 1) continue; 
                                if (diffBottom && subY <= 1) continue;
                                if (diffTop && subY >= 1) continue;
                                
                                int itemX = (worldX * 3) + subX;
                                int itemY = (worldY * 3) + subY;

                                if (Random.value < 1 - _gridItemSpawnRate) continue;

                                // --- Interactive Items ---
                                if (Random.value < _interactiveItemSpawnRate)
                                {
                                    InteractiveItem interactiveItem = GetInteractiveItemForBiomeFast(currentTile.Biome);
                                    if (interactiveItem != null && CanPlace(itemX, itemY, interactiveItem.FootPrint) && IsValidBiomeArea(itemX, itemY, interactiveItem.FootPrint, currentTile))
                                    {
                                        Vector3 worldPos = _tilemap.GetCellCenterWorld(new Vector3Int(itemX, itemY, 0));
                                        Instantiate(interactiveItem.Prefab, worldPos, Quaternion.identity, _interactiveItemsParent);
                                        Occupy(itemX, itemY, interactiveItem.FootPrint);
                                        continue;
                                    }
                                }
                                
                                // --- Common Items ---
                                if(_occupied[itemX, itemY]) continue;

                                CommonItem commonItem = null;
                                
                                if (Random.value < _gridItemBiomeSpawnRate) 
                                {
                                    List<CommonItem> biomeItems = GetCommonItemsForBiomeFast(currentTile.Biome);
                                    if (biomeItems != null && biomeItems.Count > 0)
                                    {
                                        int index = NoiseValueToIndex(noiseMap[itemX, itemY], biomeItems.Count);
                                        if(index != -1) commonItem = biomeItems[index];
                                    }
                                }
                                else
                                {
                                    int index = NoiseValueToIndex(noiseMap[itemX, itemY], _basicCommonItems.Count);
                                    if(index != -1) commonItem = _basicCommonItems[index];
                                }
                        
                                if (commonItem != null && CanPlace(itemX, itemY, commonItem.FootPrint) && IsValidBiomeArea(itemX, itemY, commonItem.FootPrint, currentTile))
                                {
                                    tilePositions.Add(new Vector3Int(itemX, itemY, 0));
                                    tiles.Add(commonItem.ItemTile);
                                    Occupy(itemX, itemY, commonItem.FootPrint);
                                }
                            }
                        }
                    }
                }
                
                if (tilePositions.Count > 0) _tilemap.SetTiles(tilePositions.ToArray(), tiles.ToArray());

                if (!skip)
                {
                    yield return null;
                }
            }
        }
    }

    private bool CanPlace(int x, int y, Vector2Int footPrint)
    {
        for (int offsetY = 0; offsetY < footPrint.y; offsetY++)
        {
            for (int offsetX = 0; offsetX < footPrint.x; offsetX++)
            {
                int checkX = x + offsetX;
                int checkY = y + offsetY;

                if (checkX < 0 || checkX >= _gridItemSize || checkY < 0 || checkY >= _gridItemSize)
                    return false;

                if (_occupied[checkX, checkY])
                    return false;
            }
        }

        return true;
    }

    private void Occupy(int x, int y, Vector2Int footPrint)
    {
        for (int offsetY = 0; offsetY < footPrint.y; offsetY++)
        {
            for (int offsetX = 0; offsetX < footPrint.x; offsetX++)
            {
                int occupyX = x + offsetX;
                int occupyY = y + offsetY;

                if (occupyX >= 0 && occupyX < _gridItemSize && occupyY >= 0 && occupyY < _gridItemSize)
                {
                    _occupied[occupyX, occupyY] = true;
                }
            }
        }
    }

    private bool IsValidBiomeArea(int startX, int startY, Vector2Int footPrint, TileData tile)
    {
        for (int offsetY = 0; offsetY < footPrint.y; offsetY++)
        {
            for (int offsetX = 0; offsetX < footPrint.x; offsetX++)
            {
                int checkWorldX = (startX + offsetX) / 3;
                int checkWorldY = (startY + offsetY) / 3;

                if (checkWorldX < 0 || checkWorldX >= _gridSize || checkWorldY < 0 || checkWorldY >= _gridSize)
                    return false;

                if(IsDifferent(tile, checkWorldX, checkWorldY))
                    return false;
            }
        }

        return true;
    }

    private bool IsDifferent(TileData currentTile, int worldX, int worldY)
    {
        TileData neighbor = GridWorld.Instance.GetTileAt(new Vector2Int(worldX, worldY));
        return neighbor.Type != currentTile.Type || neighbor.Biome != currentTile.Biome;
    }

    private InteractiveItem GetInteractiveItemForBiomeFast(BiomeType biome)
    {
        if (_biomeInteractiveItemsDict != null && _biomeInteractiveItemsDict.TryGetValue(biome, out List<InteractiveItem> items) && items != null && items.Count > 0)
        {
            return items[Random.Range(0, items.Count)];
        }
        return null;
    }

    private List<CommonItem> GetCommonItemsForBiomeFast(BiomeType biome)
    {
        if (_biomeCommonItemsDict != null && _biomeCommonItemsDict.TryGetValue(biome, out List<CommonItem> items))
        {
            return items;
        }
        return null;
    }

    private int NoiseValueToIndex(float noise, int sizeArray)
    {
        if (sizeArray <= 0)
        {
            //Debug.LogWarning("sizeArray is less than 1");
            return -1;
        }
        int index = Mathf.FloorToInt(noise * sizeArray);
        
        return Mathf.Clamp(index, 0, sizeArray - 1);
    }
}
