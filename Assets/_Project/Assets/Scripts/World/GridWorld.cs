using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class GridWorld : MonoBehaviour
{
    [SerializeField] private int _size = 100;
    [SerializeField] private float _scale = 0.1f;
    private Tile[,] _grid;
    private void Start()
    {
        Dictionary<float, Biome> biomesThresholds = new Dictionary<float, Biome>();
        Dictionary<float, TileType> tilesTypeThresholds = new Dictionary<float, TileType>();
        SetupThresholds(biomesThresholds, tilesTypeThresholds);
        
        float xoffset = Random.Range(-1000, 1000);
        float yOffset = Random.Range(-1000, 1000);
        _grid = new Tile[_size, _size];
        float[,] noiseMap = new float[_size, _size];
        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                float noiseValue = Mathf.PerlinNoise(xoffset + x * _scale, yOffset + y * _scale);
                noiseMap[x, y] = noiseValue;
            }
        }

        float[,] falloffMap = new float[_size, _size];
        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                float xv = x / (float)_size * 2 - 1;
                float yv = y / (float)_size * 2 - 1;
                float v = Mathf.Max(Mathf.Abs(xv), Mathf.Abs(yv));
                falloffMap[x, y] = Mathf.Pow(v, 3f) / (Mathf.Pow(v, 3f) + Mathf.Pow(2.2f - 2.2f * v, 3f));
            }
        }
        
        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                float noiseValue = noiseMap[x, y];
                noiseValue -= falloffMap[x, y];
                Tile tile = ComputeTile(noiseValue, biomesThresholds, tilesTypeThresholds);
                _grid[x, y] = tile;
            }
        }
    }

    private void SetupThresholds(Dictionary<float, Biome> biomesThresholds, Dictionary<float, TileType> tilesTypeThresholds)
    {
        for (int i = 1; i <= sizeof(Biome) + 1; i++)
        {
            int biome = i - 1;
            biomesThresholds.Add((float)i/(sizeof(Biome) + 1), (Biome)biome);
        }

        for (int j = 0; j <= sizeof(TileType) + 1; j++)
        {
            int type = j - 1;
            tilesTypeThresholds.Add(1f/j, (TileType) type);
        }
    }

    private Tile ComputeTile(float noiseVal, Dictionary<float, Biome> biomesThresholds, Dictionary<float, TileType> tilesTypeThresholds)
    {
        Tile tile = new Tile();
        tile.Biome = GetBiomeByNoiseValue(noiseVal, biomesThresholds);
        tile.Type = GetTileTypeByNoiseValue(noiseVal, tilesTypeThresholds);

        return tile;
    }

    private TileType GetTileTypeByNoiseValue(float noiseVal, Dictionary<float, TileType> tilesTypeThresholds)
    {
        return TileType.GRASS;
    }

    private Biome GetBiomeByNoiseValue(float noiseVal, Dictionary<float, Biome> biomesThresholds)
    {
        Biome biome = Biome.DEADZONE;
        foreach (var kvp in biomesThresholds)
        {
            biome = kvp.Value;
            if (kvp.Key > noiseVal) return biome;
        }

        return biome;
    }

    private void OnDrawGizmos()
    {
        if(!Application.isPlaying) return;

        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                Tile tile = _grid[x, y];
                switch (tile.Biome)
                {
                    case Biome.WATER:
                        Gizmos.color = Color.cornflowerBlue;
                        break;
                    case Biome.HILLS:
                        Gizmos.color = Color.springGreen;
                        break;
                    case Biome.FOREST:
                        Gizmos.color = Color.darkGreen;
                        break;
                    case Biome.DESERT:
                        Gizmos.color = Color.darkKhaki;
                        break;
                    case Biome.DEADZONE:
                        Gizmos.color = Color.white;
                        break;
                    case Biome.MOUNTAINS:
                        Gizmos.color = Color.grey;
                        break;
                }

                Vector3 pos = new Vector3(x, 0, y);
                Gizmos.DrawCube(pos, Vector3.one);
            }
        }
    }
}
