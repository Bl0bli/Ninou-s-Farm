using Godot;

/// <summary>
/// Point d'accès unique à la grille active de la scène (port direct de TileGridLocator.cs).
/// La grille s'enregistre elle-même dans _EnterTree ; les outils n'ont qu'à lire Current.
/// </summary>
public static class TileGridLocator
{
    public static ITileGrid Current { get; private set; }

    public static void Register(ITileGrid grid)
    {
        if (Current != null && Current != grid)
            GD.PushWarning($"[TileGridLocator] Une grille est déjà enregistrée ({Current}), elle est remplacée par {grid}.");

        Current = grid;
    }

    public static void Unregister(ITileGrid grid)
    {
        if (Current == grid) Current = null;
    }
}
