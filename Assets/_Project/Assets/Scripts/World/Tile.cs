using UnityEngine;

[System.Serializable]
public enum TileType
{
    DIRT,
    GRASS,
    STONE,
    WALL,
    WATER
}

[System.Serializable]
public struct Tile
{
    public Vector2 Position;
    public BiomeType Biome;
    public TileType Type;

    public Tile(Vector2 position, BiomeType biome, TileType tileType)
    {
        Position = position;
        Biome = biome;
        Type = tileType;
    }
}
