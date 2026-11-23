using Godot;

/// <summary>Graine : plante sa culture sur une case labourée libre (port de SeedData.cs).</summary>
[GlobalClass]
public partial class SeedData : ItemData, IUsable
{
    [Export] private CropData _cropData;

    public override bool IsConsumable => true;

    public bool Use(PlayerController player, Vector2 targetPosition)
    {
        ITileGrid grid = TileGridLocator.Current;
        if (grid == null || _cropData?.Scene == null) return false;

        Vector2I cell = grid.WorldToGrid(targetPosition);

        if (grid.GetTileAt(cell).State != TileState.HOED) return false;

        // Une seule culture par case.
        if (grid.IsOccupied(cell)) return false;

        // Instantiate Unity → Instantiate + AddChild : un nœud n'existe dans le monde qu'une fois dans l'arbre.
        // Rangé à côté du joueur (nœud trié en Y) pour passer devant / derrière lui selon la hauteur.
        var crop = _cropData.Scene.Instantiate<CropInstance>();
        player.WorldParent.AddChild(crop);
        crop.GlobalPosition = grid.GridToWorld(cell); // centre de la case, pas la position visée
        crop.Init(_cropData, cell, this);
        grid.TryOccupy(cell, crop);
        return true;
    }
}
