using Godot;

/// <summary>
/// Fait apparaître les hitbox d'action devant le joueur, orientées selon sa dernière direction (port de PlayerActions.cs).
/// </summary>
public partial class PlayerActions : Node
{
    [Export] private PlayerMovements _movements;
    /// <summary>Distance par défaut entre le corps et la hitbox, en cases.</summary>
    [Export] private float _defaultSpawnOffset = 0.5f;
    /// <summary>
    /// Hauteur du centre du corps au-dessus des pieds, en pixels. Dans Unity la hitbox partait de la racine du prefab,
    /// placée environ 7 px au-dessus des pieds (et 7 px à gauche, décalage non repris ici).
    /// </summary>
    [Export] private float _bodyHeight = 7f;

    public AAction SpawnAction(PackedScene scene, int damage, uint targetLayers, float duration, float spawnOffset = -1f)
    {
        if (scene == null) return null;

        Vector2 dir = _movements.LastDirection.LengthSquared() > 0.01f ? _movements.LastDirection.Normalized() : Vector2.Down;
        float offset = (spawnOffset > 0f ? spawnOffset : _defaultSpawnOffset) * PlayerMovements.PixelsPerUnit;

        var action = scene.Instantiate<AAction>();
        action.Init(damage, targetLayers, dir, duration);
        _movements.GetParent().AddChild(action);
        action.GlobalPosition = _movements.GlobalPosition + new Vector2(0, -_bodyHeight) + dir * offset;
        return action;
    }
}
