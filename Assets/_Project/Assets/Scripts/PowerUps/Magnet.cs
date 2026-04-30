using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(CircleCollider2D))]
public class Magnet : MonoBehaviour
{
    [SerializeField] private float _radius = 3f;
    [SerializeField] private float _maxForce = 8f;

    private readonly HashSet<Magnetable> _collectibles = new();

    private CircleCollider2D _collider;

    private void OnValidate()
    {
        if (_collider == null) _collider = GetComponent<CircleCollider2D>();

        if (_collider != null)
        {
            _collider.radius = _radius;
        }
    }

    private void Start()
    {
        _collider = GetComponent<CircleCollider2D>();
        _collider.isTrigger = true;
        _collider.radius = _radius;
    }

    private void FixedUpdate()
    {
        Vector2 magnetCenter = transform.position;

        foreach (Magnetable collectible in _collectibles)
        {
            if (collectible == null)
                continue;

            Transform collectibleTransform = collectible.transform;
            Vector2 currentPosition = collectibleTransform.position;

            float distance = Vector2.Distance(currentPosition, magnetCenter);

            float proximityFactor = 1f - Mathf.Clamp01(distance / _radius);
            float attractionSpeed = Mathf.Lerp(0f, _maxForce, proximityFactor);

            Vector2 newPosition = Vector2.MoveTowards(
                currentPosition,
                magnetCenter,
                attractionSpeed * Time.fixedDeltaTime
            );

            if (collectible.Rigidbody2D != null)
            {
                collectible.Rigidbody2D.MovePosition(newPosition);
            }
            else
            {
                collectibleTransform.position = newPosition;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<Magnetable>(out Magnetable magnetable))
        {
            _collectibles.Add(magnetable);
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.TryGetComponent<Magnetable>(out Magnetable magnetable))
        {
            _collectibles.Remove(magnetable);
        }
    }
}