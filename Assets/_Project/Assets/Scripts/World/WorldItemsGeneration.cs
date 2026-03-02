using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

public class WorldItemsGeneration : MonoBehaviour
{
    [SerializeField] private Tilemap _tilemap;
    [SerializeField] private List<TileBase> _basicCommonTiles;
    [SerializeField] private List<GameObject> _basicCommonInteractiveItems;
    
    [SerializeField] private float _scale = 0.1f;

    private BasicItemData[,] _gridBasicItems;
    private int _gridSize;

    public void Init()
    {
        float xoffset = Random.Range(-1000, 1000);
        float yOffset = Random.Range(-1000, 1000);
        
        _gridSize = GridWorld.Instance.GridSize;
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
                if (Random.value < 0.5f) continue;
                if (GridWorld.Instance.GetTileAt(new Vector2Int(x, y)).Type == TileType.WATER) continue;
                int index = NoiseValueToIndex(noiseMap[x,y]);
                TileBase tile = _basicCommonTiles[index];
                _tilemap.SetTile(new Vector3Int(x, y), tile);
            }
        }
    }

    private int NoiseValueToIndex(float noise)
    {
        if (_basicCommonTiles == null || _basicCommonTiles.Count == 0)
        {
            Debug.LogWarning("La liste _basicCommonTiles est vide !");
            return 0; 
        }
        
        int index = Mathf.FloorToInt(noise * _basicCommonTiles.Count);
        
        return Mathf.Clamp(index, 0, _basicCommonTiles.Count - 1);
    }
}
