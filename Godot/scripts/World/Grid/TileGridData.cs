using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Données logiques d'une grille et règles d'accès (bornes, labour, arrosage).
/// C# pur, port direct de TileGridData.cs : seul Vector2Int devient Vector2I.
/// </summary>
public class TileGridData
{
    private readonly TileData[,] _tiles;

    public int Width { get; }
    public int Height { get; }

    public event Action<Vector2I> OnTileChanged;

    /// <summary>Enveloppe un tableau de cases existant (partagé, pas copié).</summary>
    public TileGridData(TileData[,] tiles)
    {
        _tiles = tiles;
        Width = tiles.GetLength(0);
        Height = tiles.GetLength(1);
    }

    public bool IsInBounds(Vector2I p) => p.X >= 0 && p.X < Width && p.Y >= 0 && p.Y < Height;

    public TileData GetTileAt(Vector2I p)
    {
        if (IsInBounds(p)) return _tiles[p.X, p.Y];
        return new TileData(p, BiomeType.WATER, TileType.WATER);
    }

    public bool SetTileState(Vector2I p, TileState state)
    {
        if (!IsInBounds(p)) return false;
        _tiles[p.X, p.Y].State = state;
        if (state == TileState.NONE) _tiles[p.X, p.Y].IsWatered = false;

        OnTileChanged?.Invoke(p);
        return true;
    }

    public bool SetTileWatered(Vector2I p, bool isWatered)
    {
        if (!IsInBounds(p)) return false;
        _tiles[p.X, p.Y].IsWatered = isWatered;
        OnTileChanged?.Invoke(p);
        return true;
    }

    // Culture posée sur chaque case. Creux : seules les cases plantées y figurent.
    private readonly Dictionary<Vector2I, CropInstance> _crops = new();

    public bool IsOccupied(Vector2I p) => _crops.ContainsKey(p);

    public bool TryOccupy(Vector2I p, CropInstance crop) => IsInBounds(p) && _crops.TryAdd(p, crop);

    public bool TryGetCrop(Vector2I p, out CropInstance crop) => _crops.TryGetValue(p, out crop);

    public void Release(Vector2I p, CropInstance crop)
    {
        if (_crops.TryGetValue(p, out CropInstance current) && current == crop)
            _crops.Remove(p);
    }

    /// <summary>Labourable : dans la grille, sur du GROUND (ni eau, ni falaise) et pas encore labourée.</summary>
    public bool CanBeFarmed(Vector2I p)
    {
        if (!IsInBounds(p)) return false;
        TileData tile = _tiles[p.X, p.Y];
        return tile.Type == TileType.GROUND && tile.State == TileState.NONE;
    }
}
