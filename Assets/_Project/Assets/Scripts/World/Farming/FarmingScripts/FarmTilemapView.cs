using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Affiche l'état de culture des cases (labouré, arrosé) sur une Tilemap dédiée,
/// posée au-dessus du sol. Réagit aux changements notifiés par la grille active :
/// aucun outil n'a besoin de toucher à une Tilemap.
/// </summary>
public class FarmTilemapView : MonoBehaviour
{
    [SerializeField] private Tilemap _farmTilemap;
    [Tooltip("Tilemap du sol : son sprite à la case labourée choisit la tuile miroir.")]
    [SerializeField] private Tilemap _groundTilemap;
    [SerializeField] private SO_FarmTileSet _tileSet;

    private ITileGrid _grid;

    // Abonnement dans Start : les grilles s'enregistrent dans leur Awake,
    // donc TileGridLocator.Current est garanti renseigné à ce moment-là.
    private void Start()
    {
        _grid = TileGridLocator.Current;
        if (_grid == null)
        {
            Debug.LogWarning("[FarmTilemapView] Aucune grille enregistrée : l'affichage des cultures est désactivé.");
            return;
        }

        _grid.OnTileChanged += RefreshCell;
    }

    private void OnDestroy()
    {
        if (_grid != null) _grid.OnTileChanged -= RefreshCell;
    }

    private void RefreshCell(Vector2Int cell)
    {
        Vector3Int pos = new Vector3Int(cell.x, cell.y, 0);
        TileData tile = _grid.GetTileAt(cell);

        // Tuile miroir : la case labourée reprend la forme (centre, bord, coin) de la case de sol.
        Sprite groundSprite = _groundTilemap != null ? _groundTilemap.GetSprite(pos) : null;
        TileBase visual = _tileSet != null ? _tileSet.GetTile(groundSprite, tile.State, tile.IsWatered) : null;
        _farmTilemap.SetTile(pos, visual);
    }
}
