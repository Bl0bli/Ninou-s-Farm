using System;
using UnityEngine;

/// <summary>
/// Base de tout ce qui stocke des objets (inventaire du joueur, coffre, futurs stockages) :
/// un tableau de slots et les seules opérations autorisées à le modifier.
/// Les slots ne sont jamais exposés en écriture : toute modification passe par cette classe,
/// qui prévient l'affichage avec OnSlotChanged.
///
/// Un MonoBehaviour ne peut pas avoir de constructeur (Unity crée lui-même les composants) :
/// Initialize(capacity) joue ce rôle. Il est appelé dans Start avec la capacité réglée dans l'Inspector,
/// et une classe enfant peut l'appeler elle-même avec une autre taille.
/// </summary>
public abstract class Storage : MonoBehaviour
{
    [Header("Storage")]
    [Min(1)] [SerializeField] private int _capacity = 10;

    private StorageSlot[] _slots;

    /// <summary>Déclenché une fois les slots créés.</summary>
    public event Action OnInit;

    /// <summary>Déclenché après chaque modification d'un slot, avec son index.</summary>
    public event Action<int> OnSlotChanged;

    public bool IsInitialized => _slots != null;
    public int Capacity => _slots?.Length ?? 0;

    protected virtual void Start()
    {
        if (!IsInitialized) Initialize(_capacity);
    }

    /// <summary>
    /// Crée les slots vides du stockage (rôle d'un constructeur).
    /// </summary>
    /// <param name="capacity">Nombre de slots.</param>
    protected void Initialize(int capacity)
    {
        _slots = new StorageSlot[Mathf.Max(1, capacity)];
        OnInit?.Invoke();
        Debug.Log($"[Storage] {name} initialized with {Capacity} slots.");
    }

    /// <summary>
    /// Copie du slot : la modifier ne change pas le stockage.
    /// </summary>
    public StorageSlot GetSlot(int index) => IsValidIndex(index) ? _slots[index] : default;

    public bool IsValidIndex(int index) => _slots != null && index >= 0 && index < _slots.Length;

    /// <summary>
    /// Règle d'admission propre à chaque stockage. Par défaut, tout objet est accepté.
    /// Un stockage spécialisé (boîte à graines, frigo...) la surcharge.
    /// </summary>
    public virtual bool CanAccept(ItemData item) => item != null;

    /// <summary>
    /// Place disponible pour cet objet : reste des piles existantes plus slots vides.
    /// </summary>
    public int GetSpaceFor(ItemData item)
    {
        if (!IsInitialized || !CanAccept(item)) return 0;

        int space = 0;
        foreach (StorageSlot slot in _slots)
        {
            if (slot.IsEmpty) space += item.MaxStack;
            else if (slot.Item == item) space += Mathf.Max(0, item.MaxStack - slot.Quantity);
        }
        return space;
    }

    public bool CanAdd(ItemData item, int quantity) => quantity > 0 && GetSpaceFor(item) >= quantity;

    /// <summary>
    /// Ajoute une quantité : complète d'abord les piles existantes, puis remplit les slots vides.
    /// </summary>
    /// <returns>La quantité qui n'a pas pu être stockée (0 si tout est rangé).</returns>
    public int Add(ItemData item, int quantity)
    {
        if (!IsInitialized || quantity <= 0 || !CanAccept(item)) return Mathf.Max(0, quantity);

        int remaining = quantity;

        // 1. Compléter les piles du même objet, sans dépasser la taille maximale.
        for (int i = 0; i < _slots.Length && remaining > 0; i++)
        {
            if (_slots[i].IsEmpty || _slots[i].Item != item || _slots[i].IsFull) continue;

            int added = Mathf.Min(remaining, item.MaxStack - _slots[i].Quantity);
            _slots[i].Quantity += added; // écriture dans le tableau : StorageSlot est une struct
            remaining -= added;
            OnSlotChanged?.Invoke(i);
        }

        // 2. Ouvrir de nouvelles piles dans les slots vides.
        for (int i = 0; i < _slots.Length && remaining > 0; i++)
        {
            if (!_slots[i].IsEmpty) continue;

            int added = Mathf.Min(remaining, item.MaxStack);
            _slots[i].Item = item;
            _slots[i].Quantity = added;
            remaining -= added;
            OnSlotChanged?.Invoke(i);
        }

        return remaining;
    }

    /// <summary>
    /// Retire une quantité d'un slot et le vide s'il tombe à zéro.
    /// </summary>
    /// <returns>True si la quantité a pu être retirée.</returns>
    public bool Remove(int index, int quantity = 1)
    {
        if (!IsValidIndex(index) || quantity <= 0) return false;
        if (_slots[index].IsEmpty || _slots[index].Quantity < quantity) return false;

        _slots[index].Quantity -= quantity;
        if (_slots[index].Quantity <= 0) _slots[index].Clear();

        OnSlotChanged?.Invoke(index);
        return true;
    }

    /// <summary>
    /// Quantité totale d'un objet dans le stockage, toutes piles confondues.
    /// </summary>
    public int Count(ItemData item)
    {
        if (!IsInitialized || item == null) return 0;

        int total = 0;
        foreach (StorageSlot slot in _slots)
            if (!slot.IsEmpty && slot.Item == item) total += slot.Quantity;
        return total;
    }

    /// <summary>
    /// Échange le contenu de deux slots du stockage.
    /// </summary>
    public bool Swap(int a, int b)
    {
        if (!IsValidIndex(a) || !IsValidIndex(b) || a == b) return false;

        (_slots[a], _slots[b]) = (_slots[b], _slots[a]);
        OnSlotChanged?.Invoke(a);
        OnSlotChanged?.Invoke(b);
        return true;
    }

    /// <summary>
    /// Vide tous les slots.
    /// </summary>
    public void Clear()
    {
        if (!IsInitialized) return;

        for (int i = 0; i < _slots.Length; i++)
        {
            if (_slots[i].IsEmpty) continue;
            _slots[i].Clear();
            OnSlotChanged?.Invoke(i);
        }
    }
}
