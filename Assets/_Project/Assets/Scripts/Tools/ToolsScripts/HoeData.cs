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

            TileData tile = GridWorld.Instance.GetTileAt(targetPosition);
            
            //TODO si la tile est non null on essaie de la labourer (toggle)
            if (tile.Type != TileType.FLOOR) return false;
            
            //tile.
			return true;

        }
}
