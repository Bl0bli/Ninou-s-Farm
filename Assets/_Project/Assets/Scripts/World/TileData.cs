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

    /// <summary>
    /// Initialise une nouvelle instance de la structure TileData.
    /// </summary>
    /// <param name="position">La position de la tuile dans la grille.</param>
    /// <param name="biome">Le type de biome auquel appartient la tuile.</param>
    /// <param name="tileType">Le type de terrain de la tuile.</param>
    /// <param name="mask">Le masque binaire pour la configuration visuelle.</param>
    public TileData(Vector2Int position, BiomeType biome, TileType tileType, int mask = 0)
    {
        Position = position;
        Biome = biome;
        Type = tileType;
        Mask = mask;
    }
    
}
