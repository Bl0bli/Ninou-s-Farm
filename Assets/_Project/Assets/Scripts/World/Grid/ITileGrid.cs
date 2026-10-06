using System;
using UnityEngine;

/// <summary>
/// Accès en lecture/écriture à la grille logique du monde.
/// Les outils (houe, arrosoir, graines) dépendent de cette interface plutôt que d'une
/// implémentation précise : le monde procédural (GridWorld) et la scène de test (TestTileGrid)
/// peuvent ainsi fournir la grille chacun à leur manière.
/// </summary>
public interface ITileGrid
{
    /// <summary>
    /// Déclenché après chaque modification réussie de l'état d'une case (labour, arrosage).
    /// Les vues s'y abonnent pour rafraîchir l'affichage.
    /// </summary>
    event Action<Vector2Int> OnTileChanged;

    /// <summary>
    /// Convertit une position monde en coordonnée de case.
    /// </summary>
    Vector2Int WorldToGrid(Vector2 worldPosition);

    /// <summary>
    /// Renvoie les données de la case. Hors de la grille, renvoie une case WATER.
    /// </summary>
    TileData GetTileAt(Vector2Int gridPosition);

    /// <summary>
    /// Indique si la case peut être labourée.
    /// </summary>
    bool CanBeFarmed(Vector2Int gridPosition);

    /// <summary>
    /// Modifie l'état de labour de la case. Renvoie false si la case est hors de la grille.
    /// </summary>
    bool SetTileState(Vector2Int gridPosition, TileState state);

    /// <summary>
    /// Modifie l'état d'arrosage de la case. Renvoie false si la case est hors de la grille.
    /// </summary>
    bool SetTileWatered(Vector2Int gridPosition, bool isWatered);
}
