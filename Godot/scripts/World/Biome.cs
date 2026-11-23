using Godot;

/// <summary>Biomes du monde (port de Biome.cs).</summary>
public enum BiomeType
{
    WATER,
    HILLS,
    FOREST,
    DESERT,
    DEADZONE,
    MOUNTAINS
}

/// <summary>Un biome placé sur la carte : son centre (en cases) et son type.</summary>
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
