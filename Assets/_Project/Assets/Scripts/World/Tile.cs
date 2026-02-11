using UnityEngine;

[System.Serializable]
public enum Biome
{
    HILLS,
    FOREST,
    DESERT,
    DEADZONE,
    MOUNTAINS
}

public enum TileType
{
    DIRT,
    GRASS,
    STONE,
    WALL,
    WATER
}
public struct Tile
{
    public Biome Biome;
    public TileType Type;
}
