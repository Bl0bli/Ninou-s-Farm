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

    /// <summary>
    /// Réinitialise l'emplacement d'inventaire en le vidant de son contenu.
    /// </summary>
    public void Clear()
    {
        Item = null;
        Quantity = 0;
    }
}
public class PlayerInventory : MonoBehaviour
{
    [SerializeField] private ItemData[] _startingItems;

    private InventorySlot[] _inventory;
    private int _maxInventorySize = 10;

    public InventorySlot[] Inventory => _inventory;

    public event Action OnInit;
    public event Action<int> OnSlotChanged;

    private void Start()
    {
        _inventory = new InventorySlot[_maxInventorySize];
        for (int i = 0; i < _maxInventorySize; i++)
        {
            _inventory[i] = new InventorySlot();
        }

        OnInit?.Invoke();

        foreach (ItemData item in _startingItems)
        {
            if (item != null) PickUp(item);
        }
    }
    
    /// <summary>
    /// Récupère un objet et tente de le stocker dans l'inventaire.
    /// </summary>
    /// <param name="item">Les données de l'objet à ajouter.</param>
    /// <returns>Retourne true si l'objet a pu être stocké, sinon false.</returns>
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
        for (int i = 0; i < _inventory.Length; i++)
        {
            if (_inventory[i].IsEmpty)
            {
                _inventory[i].Item = item;
                _inventory[i].Quantity = item.Amount;
                OnSlotChanged?.Invoke(i);
                return true; 
            }
        }
        
        Debug.Log($"[PlayerInventory] TryAddNewItem Failed {item.Name}");
        return false;
    }

    private bool TryStack(ItemData item)
    {
        for (int i = 0; i < _inventory.Length; i++)
        {
            if (!_inventory[i].IsEmpty && !_inventory[i].IsFull && _inventory[i].Item == item)
            {
                _inventory[i].Quantity += item.Amount;
                OnSlotChanged?.Invoke(i);
                return true;
            }
        }
        Debug.Log($"[PlayerInventory] Stack Failed {item.Name}");
        return false;
    }
}
