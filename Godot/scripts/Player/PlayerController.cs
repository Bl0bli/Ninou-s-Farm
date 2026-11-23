using System;
using Godot;

/// <summary>
/// Entrées « gameplay » du joueur : objet en main, utilisation, choix du slot (port de PlayerController.cs).
///
/// Différences avec Unity :
///  - Input System + IsPointerOverUI → _UnhandledInput : Godot donne d'abord les clics à l'interface (Control),
///    seuls ceux qu'elle n'a pas consommés arrivent ici. Plus besoin de lancer un rayon sur l'UI.
///  - Le joueur a son origine aux pieds (centre du collider) : plus de décalage racine / corps à compenser.
///  - Touches 1 à 9 en plus de la molette et du clic sur une case de la toolbar.
///  - Les vues (UIInventory) sont trouvées par groupe : le joueur et l'interface sont deux scènes séparées.
/// L'interaction (touche E) est gérée par PlayerInteractor, la pêche par PlayerFishing.
/// </summary>
public partial class PlayerController : Node
{
    public const string Group = "player_controller";

    [ExportGroup("References")]
    [Export] private PlayerMovements _movements;
    [Export] private PlayerInventory _inventory;
    [Export] private AnimatedSprite2D _visual;
    [Export] private PlayerActions _actions;
    [Export] private PlayerFishing _fishing;

    [ExportGroup("Targeting")]
    /// <summary>Distance de la cible devant les pieds du joueur, en cases.</summary>
    [Export] private float _targetDistance = 1f;

    private UIInventory _inventoryView;
    private ItemData _currentItem;
    private int _currentSlotIndex;

    public event Action<int> OnSlotSelected;

    /// <summary>Sprite du personnage : les effets (eau de l'arrosoir) s'y accrochent.</summary>
    public Node2D Visual => _visual;

    /// <summary>Position des pieds du joueur (origine du CharacterBody2D).</summary>
    public Vector2 FeetPosition => _movements.GlobalPosition;

    /// <summary>Dernière direction regardée (Y vers le bas).</summary>
    public Vector2 FacingDirection => _movements.LastDirection;

    public PlayerInventory Inventory => _inventory;
    public PlayerActions Actions => _actions;
    public PlayerFishing Fishing => _fishing;
    public int CurrentSlotIndex => _currentSlotIndex;
    public ItemData CurrentItem => _currentItem;

    /// <summary>Nœud sous lequel ranger ce que le joueur fait apparaître dans le monde (cultures...).</summary>
    public Node WorldParent => _movements.GetParent();

    public override void _EnterTree() => AddToGroup(Group);

    public override void _Ready()
    {
        _inventory.OnSlotChanged += OnSlotChanged;
        _inventoryView = GetTree().GetFirstNodeInGroup(UIInventory.Group) as UIInventory;
        if (_inventoryView != null) _inventoryView.OnSelectSlot += SelectSlot;
        SelectSlot(0);
    }

    public override void _ExitTree()
    {
        if (_inventory != null) _inventory.OnSlotChanged -= OnSlotChanged;
        if (_inventoryView != null) _inventoryView.OnSelectSlot -= SelectSlot;
    }

    /// <summary>Alterne l'affichage de la grille d'inventaire.</summary>
    public void ToggleInventory() => _inventoryView?.Toggle();

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed("use")) Use();
        else if (e.IsActionPressed("toolbar_next")) Scroll(1);
        else if (e.IsActionPressed("toolbar_previous")) Scroll(-1);
        else if (e.IsActionPressed("toggle_inventory")) ToggleInventory();
        else if (e is InputEventKey { Pressed: true, Echo: false } key && TryGetNumberSlot(key.PhysicalKeycode, out int slot))
            SelectSlot(slot);
        else return;

        GetViewport().SetInputAsHandled();
    }

    /// <summary>Touches 1..9 → slots 0..8, touche 0 → slot 9 (refusé si la toolbar est plus petite).</summary>
    private static bool TryGetNumberSlot(Key key, out int slot)
    {
        slot = key == Key.Key0 ? 9 : (int)key - (int)Key.Key1;
        return key >= Key.Key0 && key <= Key.Key9;
    }

    /// <summary>Molette : le slot suivant / précédent de la toolbar, en boucle.</summary>
    private void Scroll(int step)
    {
        int maxSlots = _inventory.ToolbarSize;
        if (maxSlots <= 0) return;
        SelectSlot(((_currentSlotIndex + step) % maxSlots + maxSlots) % maxSlots);
    }

    /// <summary>Utilise l'objet tenu en main.</summary>
    public void Use()
    {
        // Pêche en cours : le clic remonte la ligne ou valide la jauge (vérifié avant IsActing, qui est vrai pendant la pêche).
        if (_fishing != null && _fishing.IsActive)
        {
            _fishing.Confirm();
            return;
        }
        if (_movements.IsActing) return; // une action à la fois (l'Animator Unity ignorait aussi le trigger)
        if (_currentItem is not IUsable usable) return;

        ItemData usedItem = _currentItem;
        bool used = usable.Use(this, GetTargetPosition());

        if (used && usedItem.IsConsumable) _inventory.Remove(_currentSlotIndex);
    }

    /// <summary>Position visée par les outils : devant les pieds du joueur, dans la dernière direction regardée.</summary>
    public Vector2 GetTargetPosition() =>
        FeetPosition + FacingDirection.Normalized() * _targetDistance * PlayerMovements.PixelsPerUnit;

    public void TriggerAnimation(string animationName) => _movements.PlayAction(animationName);

    public void SelectSlot(int index)
    {
        if (_inventory == null) return;
        // Seuls les slots de la toolbar s'équipent ; ceux de la grille servent au rangement.
        if (!_inventory.IsValidIndex(index) || index >= _inventory.ToolbarSize) return;

        _currentSlotIndex = index;
        CheckSlot();
        OnSlotSelected?.Invoke(_currentSlotIndex);
    }

    private void CheckSlot()
    {
        StorageSlot slot = _inventory.GetSlot(_currentSlotIndex);
        _currentItem = slot.IsEmpty ? null : slot.Item;
        GD.Print($"[Toolbar] Case {_currentSlotIndex} sélectionnée. En main : {(_currentItem != null ? _currentItem.Name : "Mains vides")}");
    }

    private void OnSlotChanged(int index)
    {
        if (index == _currentSlotIndex) CheckSlot();
    }
}
