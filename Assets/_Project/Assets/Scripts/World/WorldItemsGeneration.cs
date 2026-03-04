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
    
    [SerializeField] private float _scale = 0.1f;
    [SerializeField] private float _gridItemSpawnRate;
    [SerializeField] private float _gridItemBiomeSpawnRate;

    private BasicItemData[,] _gridBasicItems;
    private int _gridSize;

    public void Init()
    {
        float xoffset = Random.Range(-1000, 1000);
        float yOffset = Random.Range(-1000, 1000);

        _gridSize = GridWorld.Instance.GridSize * 4;
        _gridBasicItems = new BasicItemData[_gridSize, _gridSize];
        
        float[,] noiseMap = new float[_gridSize, _gridSize];
        for (int y = 0; y < _gridSize; y++)
        {
            for (int x = 0; x < _gridSize; x++)
            {
                noiseMap[x, y] = Mathf.PerlinNoise(xoffset + x * _scale, yOffset + y * _scale);
            }
        }

        GenerateTileMap(noiseMap);
    }

    private void GenerateTileMap(float[,] noiseMap)
    {
        for (int y = 0; y < _gridSize; y++)
        {
            for (int x = 0; x < _gridSize; x++)
            {
                if (Random.value < 1 - _gridItemSpawnRate) continue;
                TileBase tile;
                TileData tileData = GridWorld.Instance.GetTileAt(new Vector2Int(x / 4, y / 4));
                if (Random.value < 1 - _gridItemBiomeSpawnRate)
                {
                    int index = NoiseValueToIndex(noiseMap[x,y], GetSizeListOfItemsBiome(tileData.Biome));
                    if(index == -1) continue;
                    tile = GetTileToSpawnInBiome(tileData.Biome, index);
                    if (tile == null) continue;
                }
                else
                {
                    if (tileData.Type == TileType.WATER) continue;
                    int index = NoiseValueToIndex(noiseMap[x,y], _basicCommonTiles.Count);
                    tile = _basicCommonTiles[index];
                }

                _tilemap.SetTile(new Vector3Int(x, y), tile);
            }
        }
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
                return biomeItems.ItemTiles[index];
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
