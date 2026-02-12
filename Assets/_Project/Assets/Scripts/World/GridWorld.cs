using System;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using Random = UnityEngine.Random;

public class GridWorld : MonoBehaviour
{
    [SerializeField] private int _size = 100;
    [SerializeField] private float _scale = 0.1f;
    [SerializeField] private float _mixScale = 0.4f;
    [SerializeField] private float _margeX;
    [SerializeField] private float _margeY;
    private Tile[,] _grid;
    private List<Biome> _biomes = new List<Biome>();

    private int _rows, _cols;
    private void Start()
    {
        Dictionary<float, TileType> tilesTypeThresholds = new Dictionary<float, TileType>();
        SetupThresholds(tilesTypeThresholds);
        
        float xoffset = Random.Range(-1000, 1000);
        float yOffset = Random.Range(-1000, 1000);
        _grid = new Tile[_size, _size];

        SetupBiomes();
        
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
                Tile tile = ComputeTile(new Vector2(x, y), noiseValue, tilesTypeThresholds);
                _grid[x, y] = tile;
            }
        }
    }

    private void SetupBiomes()
    {
        float biomeCenterX = 0;
        float biomeCenterY = 0;
        Array allBiomeTypes = System.Enum.GetValues(typeof(BiomeType));
        List<BiomeType> validBiomes = new List<BiomeType>();

        foreach (BiomeType b in allBiomeTypes) {
            if (b != BiomeType.WATER) validBiomes.Add(b);
        }

        Shuffle(validBiomes);
        int totalBiomes = validBiomes.Count;
        _cols = Mathf.CeilToInt(Mathf.Sqrt(totalBiomes));
        _rows = Mathf.CeilToInt((float)totalBiomes / _cols);
        
        float secteurWidth = (float)_size / _cols;
        float secteurHeight = (float)_size / _rows;
        
        int biomeIndex = 0;
        for (int r = 0; r < _rows; r++) {
            for (int c = 0; c < _cols; c++) {
                if (biomeIndex >= totalBiomes) break; 

                float xMin = c * secteurWidth;
                float xMax = (c + 1) * secteurWidth;
                float yMin = r * secteurHeight;
                float yMax = (r + 1) * secteurHeight;
                biomeCenterX = Random.Range(xMin + _margeX, xMax - _margeX);
                biomeCenterY = Random.Range(yMin + _margeY, yMax - _margeY);
                _biomes.Add(new Biome(new Vector2(biomeCenterX, biomeCenterY), validBiomes[biomeIndex]));
                biomeIndex++;
            }
        }
    }
    
    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int index = Random.Range(0, i);
            (list[i], list[index]) = (list[index], list[i]);
        }
    }

    private void SetupThresholds(Dictionary<float, TileType> tilesTypeThresholds)
    {
        for (int j = 0; j <= sizeof(TileType) + 1; j++)
        {
            int type = j - 1;
            tilesTypeThresholds.Add(1f/j, (TileType) type);
        }
    }

    private Tile ComputeTile(Vector2 position, float noiseVal, Dictionary<float, TileType> tilesTypeThresholds)
    {
        Tile tile = new Tile(position, GetBiome(position), GetTileTypeByNoiseValue(noiseVal, tilesTypeThresholds));
        if (tile.Type == TileType.WATER) tile.Biome = BiomeType.WATER;
        return tile;
    }

    private TileType GetTileTypeByNoiseValue(float noiseVal, Dictionary<float, TileType> tilesTypeThresholds)
    {
        if (noiseVal < 0.2f) return TileType.WATER;
        else if (noiseVal > 0.7f) return TileType.WALL;
        return TileType.GRASS;
    }

    private BiomeType GetBiome(Vector2 position)
    {
        BiomeType biome = BiomeType.WATER;
        float minDistSq = Mathf.Infinity;

        foreach (Biome b in _biomes)
        {
            float distSq = (position - b.Position).sqrMagnitude;
            if (distSq < minDistSq)
            {
                minDistSq = distSq;
                biome = b.Type;
            }
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
                    case BiomeType.WATER:
                        Gizmos.color = Color.cornflowerBlue;
                        break;
                    case BiomeType.HILLS:
                        Gizmos.color = Color.springGreen;
                        break;
                    case BiomeType.FOREST:
                        Gizmos.color = Color.darkGreen;
                        break;
                    case BiomeType.DESERT:
                        Gizmos.color = Color.darkKhaki;
                        break;
                    case BiomeType.DEADZONE:
                        Gizmos.color = Color.white;
                        break;
                    case BiomeType.MOUNTAINS:
                        Gizmos.color = Color.grey;
                        break;
                }
                
                //float height = (tile.Type == TileType.WALL) ? 1.0f : 0f;
                Vector3 pos = new Vector3(x, 0, y);
                Gizmos.DrawCube(pos, Vector3.one);
            }
        }
    }
}
