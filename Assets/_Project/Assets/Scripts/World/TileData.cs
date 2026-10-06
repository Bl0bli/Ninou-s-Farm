using UnityEngine;

[System.Serializable]
public enum TileType
{
    GROUND,
    FLOOR,
    WATER
}

public enum TileState
{
    NONE = 0,       
    HOED = 1,
    HOED_WATERED = 2
}

[System.Serializable]
public struct TileData
{
    public Vector2Int Position;
    public BiomeType Biome;
    public TileType Type;
    public int Mask;
    public float Height;
    public bool IsWatered;
    public TileState State;

    /// <summary>
    /// Initialise une nouvelle instance de la structure TileData.
    /// </summary>
    /// <param name="position">La position de la tuile dans la grille.</param>
    /// <param name="biome">Le type de biome auquel appartient la tuile.</param>
    /// <param name="tileType">Le type de terrain de la tuile.</param>
    /// <param name="mask">Le masque binaire pour la configuration visuelle.</param>
    public TileData(Vector2Int position, BiomeType biome, TileType tileType, int mask = 0, float height = 0f, bool isWatered = false, TileState state = TileState.NONE)
    {
        Position = position;
        Biome = biome;
        Type = tileType;
        Mask = mask;
        Height = height;
        IsWatered = isWatered;
        State = state;
    }
    
}
