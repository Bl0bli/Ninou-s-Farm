using Godot;

/// <summary>
/// Affiche l'état de culture des cases (labouré, arrosé) sur un calque dédié posé au-dessus du sol
/// (port de FarmTilemapView.cs + SO_FarmTileSet.cs). Réagit aux changements notifiés par la grille :
/// aucun outil n'a besoin de toucher à un calque.
///
/// Tuile miroir : la case labourée reprend la forme (centre, bord, coin) de la case de sol. Dans Unity on
/// associait les sprites de même rectangle ; ici c'est plus direct : mêmes coordonnées d'atlas,
/// dans la source « labourée » ou « arrosée » de la feuille de sol (rule_tiles.json → farm_sheets).
/// </summary>
public partial class FarmTilemapView : TileMapLayer
{
    /// <summary>Calque du sol : sa tuile à la case labourée choisit la tuile miroir.</summary>
    [Export] private TileMapLayer _groundLayer;

    private ITileGrid _grid;

    public override void _Ready()
    {
        _grid = TileGridLocator.Current;
        if (_grid == null)
        {
            GD.PushWarning("[FarmTilemapView] Aucune grille enregistrée : l'affichage des cultures est désactivé.");
            return;
        }

        _grid.OnTileChanged += RefreshCell;
    }

    public override void _ExitTree()
    {
        if (_grid != null) _grid.OnTileChanged -= RefreshCell;
    }

    private void RefreshCell(Vector2I cell)
    {
        TileData tile = _grid.GetTileAt(cell);
        if (tile.State == TileState.NONE)
        {
            EraseCell(cell);
            return;
        }

        int groundSource = _groundLayer.GetCellSourceId(cell);
        if (!RuleTile.TryGetFarmSheet(groundSource, out RuleTile.FarmSheet sheet))
        {
            GD.PushWarning($"[FarmTilemapView] Pas de feuille labourée pour la source {groundSource} (case {cell}).");
            return;
        }

        int source = tile.IsWatered && sheet.Watered >= 0 ? sheet.Watered : sheet.Hoed;
        SetCell(cell, source, _groundLayer.GetCellAtlasCoords(cell));
    }
}
