using UnityEngine;

/// <summary>
/// Point d'accès unique à la grille active de la scène.
/// La grille s'enregistre elle-même (GridWorld dans le jeu, TestTileGrid dans la scène de test) ;
/// les outils n'ont qu'à lire Current, sans savoir laquelle est présente.
/// </summary>
public static class TileGridLocator
{
    public static ITileGrid Current { get; private set; }

    public static void Register(ITileGrid grid)
    {
        if (Current != null && Current != grid)
            Debug.LogWarning($"[TileGridLocator] Une grille est déjà enregistrée ({Current}), elle est remplacée par {grid}.");

        Current = grid;
    }

    public static void Unregister(ITileGrid grid)
    {
        if (Current == grid) Current = null;
    }

    // Sans rechargement de domaine (Enter Play Mode Options), le champ statique survivrait d'une session à l'autre.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void ResetOnLoad() => Current = null;
}
