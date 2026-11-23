using Godot;

/// <summary>Houe : laboure une case, ou arrache la culture qui s'y trouve (port direct de HoeData.cs).</summary>
[GlobalClass]
public partial class HoeData : ToolData
{
    public override bool Use(PlayerController player, Vector2 targetPosition)
    {
        if (!string.IsNullOrEmpty(_animationName))
            player.TriggerAnimation(_animationName);

        ITileGrid grid = TileGridLocator.Current;
        if (grid == null) return false;

        Vector2I cell = grid.WorldToGrid(targetPosition);

        // Une culture sur la case : la houe l'arrache (récolte ou graine), le labour reste en place.
        if (grid.TryGetCrop(cell, out CropInstance crop))
        {
            crop.Harvest();
            return true;
        }

        // Bascule : une case labourée redevient du sol, une case labourable est labourée.
        if (grid.GetTileAt(cell).State == TileState.HOED)
            return grid.SetTileState(cell, TileState.NONE);

        if (!grid.CanBeFarmed(cell)) return false;
        return grid.SetTileState(cell, TileState.HOED);
    }
}
