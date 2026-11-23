using System.Collections.Generic;
using Godot;

/// <summary>
/// Hitbox temporaire d'une action (coup de hache...) : touche une fois chaque IDamageable qu'elle chevauche,
/// puis disparaît (port d'AAction.cs).
/// Collider2D trigger + LayerMask → Area2D dont le masque de collision filtre les cibles ;
/// OnTriggerEnter2D → signal AreaEntered ; Destroy(gameObject, t) → timer puis QueueFree.
/// </summary>
public partial class AAction : Area2D
{
    [Export] private float _lastingDuration = 0.3f;
    [Export] private int _damage = 1;

    private Vector2 _attackDirection;
    private readonly HashSet<IDamageable> _alreadyHit = new();

    /// <summary>Configure la hitbox au moment de son instanciation (avant AddChild).</summary>
    public void Init(int damage, uint targetLayers, Vector2 direction, float lastingDuration)
    {
        _damage = damage;
        CollisionMask = targetLayers;
        _attackDirection = direction;
        if (lastingDuration > 0f) _lastingDuration = lastingDuration;
    }

    public override void _Ready()
    {
        AreaEntered += OnAreaEntered;
        GetTree().CreateTimer(_lastingDuration).Timeout += QueueFree;
    }

    private void OnAreaEntered(Area2D other)
    {
        IDamageable damageable = FindDamageable(other);
        if (damageable == null || !_alreadyHit.Add(damageable)) return;
        Hit(damageable);
    }

    /// <summary>TryGetComponent : la zone touchée elle-même ou son parent (la zone est souvent un enfant de l'objet).</summary>
    private static IDamageable FindDamageable(Node node) => node as IDamageable ?? node.GetParent() as IDamageable;

    /// <summary>Applique l'effet de la hitbox. À surcharger pour d'autres comportements (arroser, ramasser...).</summary>
    protected virtual void Hit(IDamageable damageable)
    {
        damageable.TakeDamage(_damage, new Attacker { AttackDirection = _attackDirection });
    }
}
