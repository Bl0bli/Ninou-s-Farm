using Godot;

/// <summary>
/// Inventaire du joueur : la grille d'UIStorage, précédée des cases de la toolbar (port d'UIInventory.cs).
/// La toolbar occupe les premiers index du stockage, la grille les suivants.
/// Dans le groupe « inventory_view » : PlayerInventory et PlayerController le trouvent sans câblage entre scènes.
/// </summary>
public partial class UIInventory : UIStorage
{
    public const string Group = "inventory_view";

    [ExportGroup("Toolbar")]
    [Export] private Container _toolbar;
    [Export] private int _toolbarSize = 9;

    [ExportGroup("Params")]
    /// <summary>Nombre de cases de la grille d'inventaire, en plus de celles de la toolbar.</summary>
    [Export] private int _startInventorySize = 36;

    /// <summary>Panneau de la grille, affiché / masqué par la touche d'inventaire.</summary>
    [Export] private Control _inventoryPanel;

    public int ToolbarSize => _toolbarSize;

    /// <summary>Nombre total de cases affichées (toolbar + grille) : la taille que doit avoir le stockage.</summary>
    public int SlotCount => ToolbarSize + _startInventorySize;

    public override void _EnterTree() => AddToGroup(Group);

    public override void _Ready()
    {
        _storage ??= GetTree().GetFirstNodeInGroup(PlayerInventory.Group) as Storage;
        if (_inventoryPanel != null) _inventoryPanel.Visible = false;
        base._Ready();
    }

    /// <summary>Affiche / masque la grille (ToogleInventory du PlayerController Unity).</summary>
    public void Toggle()
    {
        if (_inventoryPanel != null) _inventoryPanel.Visible = !_inventoryPanel.Visible;
    }

    protected override void BuildSlots()
    {
        for (int i = 0; i < ToolbarSize; i++) CreateSlot(_toolbar, i);
        for (int i = 0; i < _startInventorySize; i++) CreateSlot(_slotParent, ToolbarSize + i);
    }
}
