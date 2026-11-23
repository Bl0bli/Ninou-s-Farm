using Godot;

/// <summary>
/// Zone autour des pieds du joueur qui ramasse les objets au sol (port de PlayerCollector.cs).
/// OnTriggerEnter2D → signal AreaEntered d'une Area2D. Le rayon se règle sur la CollisionShape2D enfant.
/// </summary>
public partial class PlayerCollector : Area2D
{
    [Export] private PlayerInventory _inventory;

    public override void _Ready() => AreaEntered += OnAreaEntered;

    private void OnAreaEntered(Area2D other)
    {
        if (other is ICollectible collectible) collectible.Collect(_inventory);
    }

    /// <summary>
    /// Un objet refusé (inventaire plein) reste dans la zone : AreaEntered ne se redéclenche pas.
    /// On retente donc régulièrement sur tout ce qui chevauche la zone.
    /// </summary>
    public override void _PhysicsProcess(double delta)
    {
        if (Engine.GetPhysicsFrames() % 30 != 0) return;
        foreach (Area2D area in GetOverlappingAreas())
            if (area is ICollectible collectible) collectible.Collect(_inventory);
    }
}
