using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class AAction : MonoBehaviour
{
    [SerializeField] private LayerMask _targetLayers;
    [SerializeField] private float _lastingDuration = 0.3f;
    [SerializeField] private int _damage = 1;

    private Vector2 _attackDirection;
    private readonly HashSet<IDamageable> _alreadyHit = new HashSet<IDamageable>();

    /// <summary>
    /// Configure la hitbox au moment de son instanciation.
    /// </summary>
    public void Init(int damage, LayerMask targetLayers, Vector2 direction, float lastingDuration)
    {
        _damage = damage;
        _targetLayers = targetLayers;
        _attackDirection = direction;
        if (lastingDuration > 0f) _lastingDuration = lastingDuration;
    }

    private void Start()
    {
        Destroy(gameObject, _lastingDuration);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        Debug.Log($"[AAction] Trigger avec {other.name} (layer {LayerMask.LayerToName(other.gameObject.layer)})");
        if ((_targetLayers.value & (1 << other.gameObject.layer)) == 0) return;
        if (!other.TryGetComponent<IDamageable>(out IDamageable damageable)) return;
        if (!_alreadyHit.Add(damageable)) return;

        Hit(damageable);
    }

    /// <summary>
    /// Applique l'effet de la hitbox sur une cible. Override pour des comportements
    /// alternatifs (ex: arroser une tuile, ramasser une plante, etc).
    /// </summary>
    protected virtual void Hit(IDamageable damageable)
    {
        Attacker attacker = new Attacker { AttackDirection = _attackDirection };
        damageable.TakeDamage(_damage, attacker);
    }
}
