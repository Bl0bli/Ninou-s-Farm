using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class WorldItemsGeneration : MonoBehaviour
{
    [SerializeField] private Tilemap _tilemap;
    [SerializeField] private List<TileBase> _basicCommonTiles;
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
    private int _gridSize;
    private int _gridItemSize;
    

    /// <summary>
    /// Initialise la génération des objets et éléments de décor dans le monde.
    /// </summary>
    public void Init()
    {
        float xoffset = Random.Range(-1000, 1000);
        float yOffset = Random.Range(-1000, 1000);

        _gridSize = GridWorld.Instance.GridSize;
        _gridItemSize = _gridSize * 3;
        _gridBasicItems = new BasicItemData[_gridItemSize, _gridItemSize];
        
        float[,] noiseMap = new float[_gridItemSize, _gridItemSize];
        for (int y = 0; y < _gridItemSize; y++)
        {
            for (int x = 0; x < _gridItemSize; x++)
            {
                noiseMap[x, y] = Mathf.PerlinNoise(xoffset + x * _scale, yOffset + y * _scale);
            }
        }

        GenerateTileMap(noiseMap);
    }

    private void GenerateTileMap(float[,] noiseMap)
    {
        for (int worldY = 0; worldY < _gridSize; worldY++)
        {
            for (int worldX = 0; worldX < _gridSize; worldX++)
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

                        if (Random.value < _interactiveItemSpawnRate)
                        {
                            GameObject prefab = GetInteractivePrefabForBiome(currentTile.Biome);
                            if (prefab != null)
                            {
                                Vector3 worldPos = _tilemap.GetCellCenterWorld(new Vector3Int(itemX, itemY, 0));
                                Instantiate(prefab, worldPos, Quaternion.identity, _interactiveItemsParent);
                                continue;
                            }
                        }

                        TileBase tile = null;
                        
                        if (Random.value < _gridItemBiomeSpawnRate) 
                        {
                            int index = NoiseValueToIndex(noiseMap[itemX, itemY], GetSizeListOfItemsBiome(currentTile.Biome));
                            if(index != -1) tile = GetTileToSpawnInBiome(currentTile.Biome, index);
                        }
                        else
                        {
                            int index = NoiseValueToIndex(noiseMap[itemX, itemY], _basicCommonTiles.Count);
                            if(index != -1) tile = _basicCommonTiles[index];
                        }

                        if (tile != null)
                        {
                            _tilemap.SetTile(new Vector3Int(itemX, itemY, 0), tile);
                        }
                    }
                }
            }
        }
    }

    private bool IsDifferent(TileData currentTile, int worldX, int worldY)
    {
        TileData neighbor = GridWorld.Instance.GetTileAt(new Vector2Int(worldX, worldY));
        return neighbor.Type != currentTile.Type || neighbor.Biome != currentTile.Biome;
    }

    private GameObject GetInteractivePrefabForBiome(BiomeType biome)
    {
        foreach (BiomeInteractiveItem entry in _biomeInteractiveItems)
        {
            if (entry.Biome == biome && entry.Prefabs != null && entry.Prefabs.Count > 0)
            {
                return entry.Prefabs[Random.Range(0, entry.Prefabs.Count)];
            }
        }

        return null;
    }

    private int GetSizeListOfItemsBiome(BiomeType biome)
    {
        foreach (BiomeCommonItem biomeItems in _basicBiomeTiles)
        {
            if (biomeItems.Biome == biome)
            {
                return biomeItems.ItemTiles.Count;
            }
        }

        return 0;
    }

    private TileBase GetTileToSpawnInBiome(BiomeType biome, int index)
    {
        foreach (BiomeCommonItem biomeItems in _basicBiomeTiles)
        {
            if (biomeItems.Biome == biome)
            {
                return biomeItems.ItemTiles[index].ItemTile;
            }
        }

        return null;
    }

    private int NoiseValueToIndex(float noise, int sizeArray)
    {
        if (sizeArray <= 0)
        {
            Debug.LogWarning("sizeArray is less than 1");
            return -1;
        }
        int index = Mathf.FloorToInt(noise * sizeArray);
        
        return Mathf.Clamp(index, 0, sizeArray - 1);
    }
}
