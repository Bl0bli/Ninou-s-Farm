using System;
using UnityEngine;
using UnityEngine.Events;

public class ItemPickable : MonoBehaviour
{
    [SerializeField] UnityEvent OnPickup;
    [SerializeField] ItemData _itemData;

    private SpriteRenderer _renderer;

    private void Start()
    {
        if (_itemData != null)
        {
            _renderer = GetComponent<SpriteRenderer>();
            _renderer.sprite = _itemData.WorldSprite;
        }
    }

    public void Init(ItemData item)
    {
        _itemData = item;
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.TryGetComponent<PlayerInventory>(out PlayerInventory inventory))
        {
            Debug.Log("[ItemPickable] Hit Player, Try PickUp Item");
            if(inventory.PickUp(_itemData)) OnPickup?.Invoke();
        }
    }
}
