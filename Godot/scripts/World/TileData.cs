using Godot;

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

/// <summary>
/// Données logiques d'une case (port direct de TileData.cs).
/// Seule différence avec Unity : Vector2Int devient Vector2I, et l'axe Y de la grille pointe vers le BAS
/// (convention Godot : la case (0,0) est en haut à gauche).
/// </summary>
public struct TileData
{
    public Vector2I Position;
    public BiomeType Biome;
    public TileType Type;
    public int Mask;
    public float Height;
    public bool IsWatered;
    public TileState State;

    public TileData(Vector2I position, BiomeType biome, TileType tileType, int mask = 0, float height = 0f,
        bool isWatered = false, TileState state = TileState.NONE)
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
