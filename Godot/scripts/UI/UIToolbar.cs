using Godot;

/// <summary>
/// Déplace le sélecteur sur la case de toolbar choisie par le joueur (port d'UIToolbar.cs).
/// Les cases viennent d'UIInventory (créées en code) au lieu d'un tableau rempli dans l'Inspecteur.
/// </summary>
public partial class UIToolbar : Node
{
    [Export] private UIInventory _inventoryView;
    [Export] private MenuSelector _menuSelector;

    private PlayerController _playerController;

    public override void _Ready()
    {
        _playerController = GetTree().GetFirstNodeInGroup(PlayerController.Group) as PlayerController;
        if (_playerController == null) return;

        _playerController.OnSlotSelected += MoveSelectorVisually;
        // Le joueur a déjà choisi sa case de départ ; on attend la fin de la frame que les cases soient placées.
        Callable.From(() => MoveSelectorVisually(_playerController.CurrentSlotIndex)).CallDeferred();
    }

    public override void _ExitTree()
    {
        if (_playerController != null) _playerController.OnSlotSelected -= MoveSelectorVisually;
    }

    private void MoveSelectorVisually(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= _inventoryView.ToolbarSize) return;
        _menuSelector.AnimateSelection(_inventoryView.GetSlotView(slotIndex));
    }
}
