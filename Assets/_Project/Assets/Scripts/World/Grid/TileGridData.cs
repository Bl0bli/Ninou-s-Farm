using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Données logiques d'une grille et règles d'accès (bornes, labour, arrosage).
/// Classe C# pure, partagée par toutes les implémentations d'ITileGrid :
/// les règles métier n'existent qu'à un seul endroit.
/// </summary>
public class TileGridData
{
    private readonly TileData[,] _tiles;

    public int Width { get; }
    public int Height { get; }

    public event Action<Vector2Int> OnTileChanged;

    /// <summary>
    /// Enveloppe un tableau de cases existant. Le tableau est partagé, pas copié :
    /// une écriture faite par le générateur est visible ici, et inversement.
    /// </summary>
    public TileGridData(TileData[,] tiles)
    {
        _tiles = tiles;
        Width = tiles.GetLength(0);
        Height = tiles.GetLength(1);
    }

    public bool IsInBounds(Vector2Int gridPosition) =>
        gridPosition.x >= 0 && gridPosition.x < Width && gridPosition.y >= 0 && gridPosition.y < Height;

    public TileData GetTileAt(Vector2Int gridPosition)
    {
        if (IsInBounds(gridPosition))
            return _tiles[gridPosition.x, gridPosition.y];
        return new TileData(gridPosition, BiomeType.WATER, TileType.WATER);
    }

    public bool SetTileState(Vector2Int gridPosition, TileState state)
    {
        if (!IsInBounds(gridPosition)) return false;
        _tiles[gridPosition.x, gridPosition.y].State = state;
        OnTileChanged?.Invoke(gridPosition);
        return true;
    }

    public bool SetTileWatered(Vector2Int gridPosition, bool isWatered)
    {
        if (!IsInBounds(gridPosition)) return false;
        _tiles[gridPosition.x, gridPosition.y].IsWatered = isWatered;
        OnTileChanged?.Invoke(gridPosition);
        return true;
    }

    // Culture posée sur chaque case. Creux : seules les cases plantées y figurent.
    private readonly Dictionary<Vector2Int, CropInstance> _crops = new Dictionary<Vector2Int, CropInstance>();

    public bool IsOccupied(Vector2Int gridPosition) => _crops.ContainsKey(gridPosition);

    /// <summary>
    /// Enregistre la culture sur la case. Renvoie false si elle est hors grille ou déjà occupée :
    /// le test et l'enregistrement sont faits ensemble, deux plantations ne peuvent pas se croiser.
    /// </summary>
    public bool TryOccupy(Vector2Int gridPosition, CropInstance crop) =>
        IsInBounds(gridPosition) && _crops.TryAdd(gridPosition, crop);

    public bool TryGetCrop(Vector2Int gridPosition, out CropInstance crop) => _crops.TryGetValue(gridPosition, out crop);

    /// <summary>
    /// Libère la case, uniquement si c'est bien cette culture qui l'occupe.
    /// </summary>
    public void Release(Vector2Int gridPosition, CropInstance crop)
    {
        if (_crops.TryGetValue(gridPosition, out CropInstance current) && current == crop)
            _crops.Remove(gridPosition);
    }

    /// <summary>
    /// Une case est labourable si elle est dans la grille, sur un sol GROUND
    /// (ni eau, ni falaise) et pas encore labourée.
    /// </summary>
    public bool CanBeFarmed(Vector2Int gridPosition)
    {
        if (!IsInBounds(gridPosition)) return false;

        TileData tile = _tiles[gridPosition.x, gridPosition.y];
        return tile.Type == TileType.GROUND && tile.State == TileState.NONE;
    }
}
