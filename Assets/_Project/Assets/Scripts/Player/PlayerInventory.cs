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
    
    /// <summary>
    /// Retire une quantité d'un emplacement et vide l'emplacement s'il tombe à zéro.
    /// </summary>
    /// <param name="index">L'index de l'emplacement.</param>
    /// <param name="quantity">La quantité à retirer.</param>
    /// <returns>Retourne true si la quantité a pu être retirée, sinon false.</returns>
    public bool Remove(int index, int quantity = 1)
    {
        if (_inventory == null || index < 0 || index >= _inventory.Length) return false;
        if (_inventory[index].IsEmpty || _inventory[index].Quantity < quantity) return false;

        // Écriture directe dans le tableau : InventorySlot est une struct, une copie locale serait perdue.
        _inventory[index].Quantity -= quantity;
        if (_inventory[index].Quantity <= 0) _inventory[index].Clear();

        OnSlotChanged?.Invoke(index);
        return true;
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
