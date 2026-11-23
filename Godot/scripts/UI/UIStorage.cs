using System;
using System.Collections.Generic;
using Godot;

/// <summary>
/// Affichage d'un Storage sous forme de grille de cases, avec les animations d'ajout et d'empilement
/// (port d'UIStorage.cs). Base commune de l'inventaire du joueur (UIInventory) et du coffre (UIChest).
/// La case affichée i est toujours le slot i du stockage.
/// </summary>
public partial class UIStorage : Control
{
    private const string SlotScenePath = "res://scenes/ui/ui_slot.tscn";

    [ExportGroup("References")]
    /// <summary>Stockage affiché. Peut rester vide si la vue est reliée par code (Bind).</summary>
    [Export] protected Storage _storage;
    /// <summary>Conteneur des cases de la grille (GridContainer : remplace le GridLayoutGroup Unity).</summary>
    [Export] protected Container _slotParent;

    private readonly List<UISlot> _slots = new();
    private readonly List<UISlot> _createdSlots = new();
    private PackedScene _slotScene;
    private bool _animate;

    /// <summary>Clic sur une case, avec l'index du stockage correspondant.</summary>
    public event Action<int> OnSelectSlot;

    public Storage Storage => _storage;

    public override void _Ready()
    {
        MouseFilter = MouseFilterEnum.Ignore;
        _slotScene = GD.Load<PackedScene>(SlotScenePath);
        Bind(_storage);
    }

    public override void _ExitTree() => Unsubscribe();

    /// <summary>Relie la vue à un autre stockage (un coffre qu'on ouvre, par exemple) et reconstruit la grille.</summary>
    public void Bind(Storage storage)
    {
        Unsubscribe();
        _storage = storage;
        Subscribe();
        if (_storage != null && _storage.IsInitialized) Rebuild();
    }

    /// <summary>Case affichée pour un index du stockage (pour placer le sélecteur de la toolbar).</summary>
    public UISlot GetSlotView(int index) => index >= 0 && index < _slots.Count ? _slots[index] : null;

    private void Subscribe()
    {
        if (_storage == null) return;
        _storage.OnSlotChanged += UpdateSingleSlot;
        _storage.OnInit += Rebuild;
    }

    private void Unsubscribe()
    {
        if (_storage == null) return;
        _storage.OnSlotChanged -= UpdateSingleSlot;
        _storage.OnInit -= Rebuild;
    }

    /// <summary>Recrée les cases pour le stockage courant, puis les remplit sans animation.</summary>
    private void Rebuild()
    {
        foreach (UISlot created in _createdSlots) created.QueueFree();
        _createdSlots.Clear();
        _slots.Clear();

        BuildSlots();

        _animate = false;
        for (int i = 0; i < _slots.Count; i++) UpdateSingleSlot(i);
        _animate = true;
    }

    /// <summary>Une case de grille par slot du stockage. UIInventory la surcharge pour placer la toolbar en premier.</summary>
    protected virtual void BuildSlots()
    {
        for (int i = 0; i < _storage.Capacity; i++) CreateSlot(_slotParent, i);
    }

    /// <summary>Instancie une case dans un conteneur, reliée à l'index donné du stockage.</summary>
    protected void CreateSlot(Container parent, int storageIndex)
    {
        var slot = _slotScene.Instantiate<UISlot>();
        parent.AddChild(slot);
        _createdSlots.Add(slot);
        _slots.Add(slot);
        slot.Bind(this, storageIndex);
        slot.Pressed += () => OnSelectSlot?.Invoke(storageIndex);
    }

    private void UpdateSingleSlot(int index)
    {
        if (index < 0 || index >= _slots.Count) return;

        UISlot view = _slots[index];
        StorageSlot slot = _storage.GetSlot(index);
        if (slot.IsEmpty)
        {
            view.Clear();
            return;
        }

        bool isNewItem = !view.HasIcon;
        view.SetItem(slot.Item.Icon, slot.Quantity);

        if (!_animate) return;
        if (isNewItem) view.AnimateNewItem();
        else view.AnimateQuantity();
    }
}
