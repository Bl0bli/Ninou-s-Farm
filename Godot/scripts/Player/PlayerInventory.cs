using System;
using Godot;

/// <summary>
/// Inventaire du joueur : un Storage, plus les objets de départ et le ramassage (port de PlayerInventory.cs).
/// Sa taille vient de l'interface : autant de slots que la toolbar et la grille en affichent.
/// Sans UIInventory dans la scène, la capacité et la toolbar réglées dans l'Inspecteur sont utilisées.
/// </summary>
public partial class PlayerInventory : Storage
{
    public const string Group = "player_inventory";

    [Export] private ItemData[] _startingItems = Array.Empty<ItemData>();

    /// <summary>Taille de la toolbar sans interface (sinon, celle d'UIInventory).</summary>
    [Export] private int _toolbarSize = 9;

    public int ToolbarSize { get; private set; }

    public override void _EnterTree() => AddToGroup(Group);

    public override void _Ready()
    {
        // Les nœuds des groupes existent dès l'entrée dans l'arbre : l'interface est trouvée même si son _Ready n'a pas encore eu lieu.
        var view = GetTree().GetFirstNodeInGroup(UIInventory.Group) as UIInventory;
        if (view != null) Initialize(view.SlotCount);
        else GD.PushWarning("[PlayerInventory] Aucun UIInventory trouvé : la capacité de l'Inspecteur est utilisée.");

        base._Ready(); // n'initialise qu'à défaut, avec la capacité de l'Inspecteur
        ToolbarSize = Math.Min(view?.ToolbarSize ?? _toolbarSize, Capacity);

        foreach (ItemData item in _startingItems)
        {
            if (item != null) PickUp(item);
        }
    }

    /// <summary>
    /// Récupère un objet et tente de le stocker. Tout ou rien : si la quantité ne tient pas entièrement, rien n'est ajouté.
    /// </summary>
    public bool PickUp(ItemData item)
    {
        if (item == null) return false;

        int quantity = Math.Max(1, item.Amount);
        if (!CanAdd(item, quantity))
        {
            GD.Print($"[PlayerInventory] Plus de place pour {item.Name}");
            return false;
        }

        Add(item, quantity);
        return true;
    }
}
