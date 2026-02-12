using UnityEngine;

[System.Serializable]
public enum TileType
{
    GROUND,
    FLOOR,
    WATER
}

[System.Serializable]
public struct TileData
{
    public Vector2Int Position;
    public BiomeType Biome;
    public TileType Type;
    public int Mask;

    public TileData(Vector2Int position, BiomeType biome, TileType tileType, int mask = 0)
    {
        Position = position;
        Biome = biome;
        Type = tileType;
        Mask = mask;
    }
}
