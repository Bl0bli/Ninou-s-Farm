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

    /// <summary>
    /// Initialise une nouvelle instance de la structure Biome.
    /// </summary>
    /// <param name="pos">La position centrale du biome.</param>
    /// <param name="biomeType">Le type de biome.</param>
    public Biome(Vector2 pos, BiomeType biomeType)
    {
        Position = pos;
        Type = biomeType;
    }
}