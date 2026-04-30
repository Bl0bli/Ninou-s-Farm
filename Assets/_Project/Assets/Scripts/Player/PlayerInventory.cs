using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;

public struct InventorySlot
{
    public ItemData Item;
    public int Quantity;
    public bool IsEmpty => Item == null || Quantity <= 0;
    public bool IsFull => Quantity >= 99;

    public void Clear()
    {
        Item = null;
        Quantity = 0;
    }
}
public class PlayerInventory : MonoBehaviour
{
    private InventorySlot[] _inventory;
    private int _maxInventorySize = 10;
    
    public InventorySlot[] Inventory => _inventory;

    private void Start()
    {
        _inventory = new InventorySlot[_maxInventorySize];
        for (int i = 0; i < _maxInventorySize; i++)
        {
            _inventory[i] = new InventorySlot();
        }    }

    public bool PickUp(ItemData item)
    {
        if (TryStack(item))
        {
            return true;
        }

        return TryAddNewItem(item);
    }
    
    private bool TryAddNewItem(ItemData item)
    {
        Debug.Log($"[PlayerInventory] TryAddNewItem {item.Name}");
        for (int i = 0; i < _inventory.Length; i++)
        {
            if (_inventory[i].IsEmpty)
            {
                _inventory[i].Item = item;
                _inventory[i].Quantity = item.Amount;
                // TODO : Notifier l'UI de se mettre à jour
                Debug.Log($"[PlayerInventory] Added {item.Name}");
                return true; 
            }
        }
        
        Debug.Log($"[PlayerInventory] TryAddNewItem Failed {item.Name}");
        return false;
    }

    private bool TryStack(ItemData item)
    {
        Debug.Log($"[PlayerInventory] TryStack {item.Name}");
        for (int i = 0; i < _inventory.Length; i++)
        {
            InventorySlot slot = _inventory[i];
            if (!slot.IsEmpty && !slot.IsFull && slot.Item == item)
            {
                Debug.Log($"[PlayerInventory] Stack {item.Name}");
                return true;
            }
        }
        Debug.Log($"[PlayerInventory] Stack Failed {item.Name}");
        return false;
    }
}
