using System.Collections.Generic;
using Godot;

/// <summary>
/// Aimant du joueur : attire les objets Magnetable dans son rayon, d'autant plus vite qu'ils sont proches
/// (port de Magnet.cs). Le rayon se règle ici et s'applique au CircleShape2D enfant.
/// </summary>
public partial class Magnet : Area2D
{
    /// <summary>Rayon d'attraction, en cases (2 dans le prefab Player).</summary>
    [Export] private float _radius = 2f;
    /// <summary>Vitesse maximale d'attraction, en cases par seconde.</summary>
    [Export] private float _maxForce = 8f;
    [Export] private CollisionShape2D _shape;

    private readonly HashSet<Magnetable> _collectibles = new();

    private float RadiusPixels => _radius * PlayerMovements.PixelsPerUnit;

    public override void _Ready()
    {
        if (_shape?.Shape is CircleShape2D circle) circle.Radius = RadiusPixels;

        AreaEntered += area =>
        {
            Magnetable m = Magnetable.FindIn(area);
            if (m != null) _collectibles.Add(m);
        };
        AreaExited += area =>
        {
            Magnetable m = Magnetable.FindIn(area);
            if (m != null) _collectibles.Remove(m);
        };
    }

    public override void _PhysicsProcess(double delta)
    {
        Vector2 center = GlobalPosition;
        _collectibles.RemoveWhere(m => !IsInstanceValid(m)); // objets ramassés entre-temps

        foreach (Magnetable collectible in _collectibles)
        {
            Node2D target = collectible.Target;
            float distance = target.GlobalPosition.DistanceTo(center);
            float proximity = 1f - Mathf.Clamp(distance / RadiusPixels, 0f, 1f);
            float speed = Mathf.Lerp(0f, _maxForce * PlayerMovements.PixelsPerUnit, proximity);

            target.GlobalPosition = target.GlobalPosition.MoveToward(center, speed * (float)delta);
        }
    }
}
