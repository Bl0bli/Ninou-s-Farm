using System;
using Godot;

/// <summary>
/// Accès en lecture/écriture à la grille logique du monde (port direct d'ITileGrid.cs).
/// Les outils (houe, arrosoir, graines) dépendent de cette interface : le monde procédural (GridWorld, à porter)
/// et la scène de test (TestTileGrid) fournissent la grille chacun à leur manière.
/// </summary>
public interface ITileGrid
{
    /// <summary>Déclenché après chaque modification réussie de l'état d'une case (labour, arrosage).</summary>
    event Action<Vector2I> OnTileChanged;

    /// <summary>Convertit une position globale (pixels) en coordonnée de case.</summary>
    Vector2I WorldToGrid(Vector2 worldPosition);

    /// <summary>Position globale (pixels) du centre d'une case.</summary>
    Vector2 GridToWorld(Vector2I gridPosition);

    bool IsOccupied(Vector2I gridPosition);
    bool TryOccupy(Vector2I gridPosition, CropInstance crop);
    bool TryGetCrop(Vector2I gridPosition, out CropInstance crop);
    void Release(Vector2I gridPosition, CropInstance crop);

    /// <summary>Renvoie les données de la case. Hors de la grille, renvoie une case WATER.</summary>
    TileData GetTileAt(Vector2I gridPosition);

    bool CanBeFarmed(Vector2I gridPosition);
    bool SetTileState(Vector2I gridPosition, TileState state);
    bool SetTileWatered(Vector2I gridPosition, bool isWatered);
}
