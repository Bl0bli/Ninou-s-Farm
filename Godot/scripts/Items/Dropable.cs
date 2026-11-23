using Godot;

/// <summary>
/// Objet du monde qui lâche un objet ramassable (port de Dropable.cs).
/// Resources.Load("Prefabs/ItemPickable") → GD.Load d'une scène, puis Instantiate + AddChild.
/// </summary>
public partial class Dropable : Node2D
{
    private const string PickableScenePath = "res://scenes/item_pickable.tscn";

    [Export] protected ItemData _itemDropped;

    protected virtual void DropItem(bool destroySelf = false)
    {
        var pickable = GD.Load<PackedScene>(PickableScenePath).Instantiate<ItemPickable>();
        pickable.Init(_itemDropped);

        // Ajouté au même parent : il reste dans le monde même si cet objet disparaît.
        // CallDeferred : on peut être appelé pendant un calcul physique, où l'arbre ne doit pas changer.
        GetParent().CallDeferred(Node.MethodName.AddChild, pickable);
        pickable.Position = Position;

        if (destroySelf) QueueFree();
    }
}
