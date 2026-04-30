using System;
using UnityEngine;

[RequireComponent(typeof(PlayerInventory), typeof(CircleCollider2D))]
public class PlayerCollector : MonoBehaviour
{
    [SerializeField] private float _radius = 3f;
    [SerializeField] private PlayerInventory _inventory;
    
    private CircleCollider2D _collider;

    private void OnValidate()
    {
        if(_collider == null) _collider = GetComponent<CircleCollider2D>();
        if (_collider != null)
        {
            _collider.radius = _radius;
        }
    }

    private void Start()
    {
        if(_inventory ==null) _inventory = GetComponent<PlayerInventory>();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<ICollectible>(out ICollectible collectible))
        {
            collectible.Collect(_inventory);
        }
    }
}
