using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Items/ItemData/SeedData")]
public class SeedData : ItemData, IUsable
{
    [SerializeField] private CropData _cropData;

    public override bool IsConsumable => true;    

    public bool Use(PlayerController player, Vector2 targetPosition)
    {
        /*if (!string.IsNullOrEmpty(_animationTriggerName))
        {
            player.TriggerAnimation(_animationTriggerName);
        }*/

        ITileGrid grid = TileGridLocator.Current;
        if (grid == null) return false;

        Vector2Int cell = grid.WorldToGrid(targetPosition);

        if (grid.GetTileAt(cell).State != TileState.HOED) return false;

        // Une seule culture par case.
        if (grid.IsOccupied(cell)) return false;

        // Centre de la case, pas la position visée par le joueur.
        CropInstance crop = Instantiate(_cropData.Prefab, grid.GridToWorld(cell), Quaternion.identity).GetComponent<CropInstance>();
        crop.Init(_cropData, cell);
        grid.TryOccupy(cell, crop);
        return true;
    }
}
