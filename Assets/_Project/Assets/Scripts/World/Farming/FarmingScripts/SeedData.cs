using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Items/ItemData/SeedData")]
public class SeedData : ItemData, IUsable
{
    [SerializeField] private CropData _cropData;    

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

        if (!grid.TryOccupy(cell)) return false;

        CropInstance crop = Instantiate(_cropData.Prefab, grid.GridToWorld(cell), Quaternion.identity).GetComponent<CropInstance>();
        crop.Init(_cropData, cell);
        return true;
    }
}
