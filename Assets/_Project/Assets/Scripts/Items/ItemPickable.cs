using System;
using UnityEngine;
using UnityEngine.Events;

public class ItemPickable : MonoBehaviour, ICollectible
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] UnityEvent OnPickup;
    [SerializeField] ItemData _itemData;

    private void Start()
    {
        SetupItemPickable();
    }

    public void Init(ItemData item)
    {
        _itemData = item;
        SetupItemPickable();
    }

    private void SetupItemPickable()
    {
        if (_itemData != null)
        {
            if(_renderer == null)_renderer = GetComponent<SpriteRenderer>();
            _renderer.sprite = _itemData.WorldSprite;
        }
    }
    
    public void Collect(PlayerInventory inventory)
    {
        if (inventory.PickUp(_itemData))
        {
            OnPickup?.Invoke();
            
            Destroy(gameObject);
        }
    }
}
