using Godot;

/// <summary>Canne à pêche : lance la ligne vers une case d'eau (port de FishingRodData.cs).</summary>
[GlobalClass]
public partial class FishingRodData : ToolData
{
    [ExportGroup("Fishing Settings")]
    [Export] private FishLootTable _lootTable;

    /// <summary>Délai avant la morsure (min, max), en secondes. Le minimum doit couvrir l'animation de lancer (~1 s).</summary>
    [Export] private Vector2 _biteDelay = new(2f, 5f);

    /// <summary>Temps laissé au joueur pour valider la jauge une fois que le poisson mord, en secondes.</summary>
    [Export(PropertyHint.Range, "0.5,30,or_greater")] private float _hitWindow = 6f;

    public FishLootTable LootTable => _lootTable;
    public float HitWindow => _hitWindow;

    public float RollBiteDelay() =>
        (float)GD.RandRange(Mathf.Min(_biteDelay.X, _biteDelay.Y), Mathf.Max(_biteDelay.X, _biteDelay.Y));

    /// <summary>Lance la canne vers la case visée : la pêche démarre si c'est de l'eau.</summary>
    /// <returns>True si une pêche a démarré (false : case de sol, pêche déjà en cours).</returns>
    public override bool Use(PlayerController player, Vector2 targetPosition)
    {
        ITileGrid grid = TileGridLocator.Current;
        if (grid == null || player.Fishing == null) return false;

        Vector2I cell = grid.WorldToGrid(targetPosition);
        if (grid.GetTileAt(cell).Type != TileType.WATER) return false;

        return player.Fishing.TryBegin(this, cell);
    }
}
