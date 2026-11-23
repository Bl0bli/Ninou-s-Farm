using System;
using Godot;

/// <summary>
/// Base de tout ce qui stocke des objets (inventaire du joueur, coffre...) : port de Storage.cs.
/// Un tableau de slots et les seules opérations autorisées à le modifier ; l'affichage est prévenu par OnSlotChanged.
///
/// Node2D (et pas Node) : un coffre est un Storage posé dans le monde, il lui faut une position.
/// Comme un MonoBehaviour, un Node n'a pas de constructeur utilisable depuis la scène :
/// Initialize(capacity) joue ce rôle, appelé dans _Ready avec la capacité réglée dans l'Inspecteur.
/// </summary>
public abstract partial class Storage : Node2D
{
    [ExportGroup("Storage")]
    [Export(PropertyHint.Range, "1,999")] private int _capacity = 10;

    private StorageSlot[] _slots;

    /// <summary>Déclenché une fois les slots créés.</summary>
    public event Action OnInit;

    /// <summary>Déclenché après chaque modification d'un slot, avec son index.</summary>
    public event Action<int> OnSlotChanged;

    public bool IsInitialized => _slots != null;
    public int Capacity => _slots?.Length ?? 0;

    public override void _Ready()
    {
        if (!IsInitialized) Initialize(_capacity);
    }

    /// <summary>Crée les slots vides du stockage (rôle d'un constructeur).</summary>
    protected void Initialize(int capacity)
    {
        _slots = new StorageSlot[Math.Max(1, capacity)];
        OnInit?.Invoke();
        GD.Print($"[Storage] {Name} initialized with {Capacity} slots.");
    }

    /// <summary>Copie du slot : la modifier ne change pas le stockage.</summary>
    public StorageSlot GetSlot(int index) => IsValidIndex(index) ? _slots[index] : default;

    public bool IsValidIndex(int index) => _slots != null && index >= 0 && index < _slots.Length;

    /// <summary>Règle d'admission propre à chaque stockage. Par défaut, tout objet est accepté.</summary>
    public virtual bool CanAccept(ItemData item) => item != null;

    /// <summary>Place disponible pour cet objet : reste des piles existantes plus slots vides.</summary>
    public int GetSpaceFor(ItemData item)
    {
        if (!IsInitialized || !CanAccept(item)) return 0;

        int space = 0;
        foreach (StorageSlot slot in _slots)
        {
            if (slot.IsEmpty) space += item.MaxStack;
            else if (slot.Item == item) space += Math.Max(0, item.MaxStack - slot.Quantity);
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
        if (!IsInitialized || quantity <= 0 || !CanAccept(item)) return Math.Max(0, quantity);

        int remaining = quantity;

        // 1. Compléter les piles du même objet, sans dépasser la taille maximale.
        for (int i = 0; i < _slots.Length && remaining > 0; i++)
        {
            if (_slots[i].IsEmpty || _slots[i].Item != item || _slots[i].IsFull) continue;

            int added = Math.Min(remaining, item.MaxStack - _slots[i].Quantity);
            _slots[i].Quantity += added; // écriture dans le tableau : StorageSlot est une struct
            remaining -= added;
            OnSlotChanged?.Invoke(i);
        }

        // 2. Ouvrir de nouvelles piles dans les slots vides.
        for (int i = 0; i < _slots.Length && remaining > 0; i++)
        {
            if (!_slots[i].IsEmpty) continue;

            int added = Math.Min(remaining, item.MaxStack);
            _slots[i].Item = item;
            _slots[i].Quantity = added;
            remaining -= added;
            OnSlotChanged?.Invoke(i);
        }

        return remaining;
    }

    /// <summary>Retire une quantité d'un slot et le vide s'il tombe à zéro.</summary>
    public bool Remove(int index, int quantity = 1)
    {
        if (!IsValidIndex(index) || quantity <= 0) return false;
        if (_slots[index].IsEmpty || _slots[index].Quantity < quantity) return false;

        _slots[index].Quantity -= quantity;
        if (_slots[index].Quantity <= 0) _slots[index].Clear();

        OnSlotChanged?.Invoke(index);
        return true;
    }

    /// <summary>Quantité totale d'un objet dans le stockage, toutes piles confondues.</summary>
    public int Count(ItemData item)
    {
        if (!IsInitialized || item == null) return 0;

        int total = 0;
        foreach (StorageSlot slot in _slots)
            if (!slot.IsEmpty && slot.Item == item) total += slot.Quantity;
        return total;
    }

    /// <summary>Échange le contenu de deux slots du stockage.</summary>
    public bool Swap(int a, int b)
    {
        if (!IsValidIndex(a) || !IsValidIndex(b) || a == b) return false;

        (_slots[a], _slots[b]) = (_slots[b], _slots[a]);
        OnSlotChanged?.Invoke(a);
        OnSlotChanged?.Invoke(b);
        return true;
    }

    /// <summary>
    /// Déplace le contenu d'un slot vers un slot d'un autre stockage (ou du même) : ajout du port Godot,
    /// utilisé par le glisser-déposer. Même objet des deux côtés : complète la pile d'arrivée sans dépasser
    /// la taille maximale (le reste reste au départ). Sinon : échange les deux slots, si chaque stockage
    /// accepte l'objet qu'il reçoit.
    /// </summary>
    /// <returns>True si quelque chose a bougé.</returns>
    public bool MoveTo(int fromIndex, Storage target, int toIndex)
    {
        if (target == null || !IsValidIndex(fromIndex) || !target.IsValidIndex(toIndex)) return false;
        if (target == this && fromIndex == toIndex) return false;

        StorageSlot from = _slots[fromIndex];
        StorageSlot to = target._slots[toIndex]; // même classe : accès aux slots privés de l'autre stockage
        if (from.IsEmpty) return false;

        if (!to.IsEmpty && to.Item == from.Item)
        {
            int moved = Math.Min(from.Quantity, from.Item.MaxStack - to.Quantity);
            if (moved <= 0) return false;

            target._slots[toIndex].Quantity += moved;
            _slots[fromIndex].Quantity -= moved;
            if (_slots[fromIndex].Quantity <= 0) _slots[fromIndex].Clear();
        }
        else
        {
            if (!target.CanAccept(from.Item) || (!to.IsEmpty && !CanAccept(to.Item))) return false;

            _slots[fromIndex] = to;
            target._slots[toIndex] = from;
        }

        OnSlotChanged?.Invoke(fromIndex);
        target.OnSlotChanged?.Invoke(toIndex);
        return true;
    }

    /// <summary>Vide tous les slots.</summary>
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
