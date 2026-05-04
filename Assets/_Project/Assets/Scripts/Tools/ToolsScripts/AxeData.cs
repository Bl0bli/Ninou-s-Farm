using UnityEngine;

[CreateAssetMenu(fileName = "AxeData", menuName = "Items/Tools/AxeData")]
public class AxeData : ToolData
{
    [Header("Specific Settings")] 
    [SerializeField] private LayerMask _treeLayer;
    [SerializeField] private int _damage;
    [SerializeField] private float _hitRadius;

    /// <summary>
    /// Utilise la hache pour frapper et infliger des dégâts aux objets destructibles.
    /// </summary>
    /// <param name="player">Le contrôleur du joueur utilisant la hache.</param>
    /// <param name="targetPosition">La position visée par le coup de hache.</param>
    /// <returns>Retourne true si une cible a été touchée, sinon false.</returns>
    public override bool Use(PlayerController player, Vector2 targetPosition)
    {
        Collider2D hit = Physics2D.OverlapCircle(targetPosition, _hitRadius, _treeLayer);

        if (hit != null)
        {
            if (hit.TryGetComponent<IDamageable>(out IDamageable damageable))
            {
                damageable.TakeDamage(_damage);
                Debug.Log($"[AxeData] Used on {hit.name} and took {_damage} damage");
                return true;
            }
        }
        Debug.Log("[AxeData] Coup dans le vide...");
        return false;
    }
}
