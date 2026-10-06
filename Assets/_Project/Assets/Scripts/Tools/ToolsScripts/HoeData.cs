using UnityEngine;

[CreateAssetMenu(fileName = "HoeData", menuName = "Items/Tools/HoeData")]

public class HoeData : ToolData
{
        [Header("Specific Settings")] 
        [SerializeField] private LayerMask _treeLayer;
        [SerializeField] private int _damage;
        [SerializeField] private float _hitRadius;
    
        /// <summary>
        /// Utilise la houe pour labourer le sol.
        /// </summary>
        /// <param name="player">Le contrôleur du joueur utilisant la houe.</param>
        /// <param name="targetPosition">La position visée sur la grille.</param>
        /// <returns>Retourne true si une tile a été altérée, sinon false.</returns>
        public override bool Use(PlayerController player, Vector2 targetPosition)
        {
            if (!string.IsNullOrEmpty(_animationTriggerName))
            {
                player.TriggerAnimation(_animationTriggerName);
            }

            ITileGrid grid = TileGridLocator.Current;
            if (grid == null) return false;

            Vector2Int cell = grid.WorldToGrid(targetPosition);

            // Bascule : une case labourée redevient du sol, une case labourable est labourée.
            if (grid.GetTileAt(cell).State == TileState.HOED)
                return grid.SetTileState(cell, TileState.NONE);

            if (!grid.CanBeFarmed(cell)) return false;
            return grid.SetTileState(cell, TileState.HOED);

        }
}
