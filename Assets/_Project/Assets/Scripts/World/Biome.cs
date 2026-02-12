using UnityEngine;

[System.Serializable]
public enum BiomeType
{
    WATER,
    HILLS,
    FOREST,
    DESERT,
    DEADZONE,
    MOUNTAINS
}

[System.Serializable]
public struct Biome
{
    public Vector2 Position;
    public BiomeType Type;

    public Biome(Vector2 pos, BiomeType biomeType)
    {
        Position = pos;
        Type = biomeType;
    }
}