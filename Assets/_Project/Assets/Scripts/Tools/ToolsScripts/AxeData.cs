using UnityEngine;

[CreateAssetMenu(fileName = "AxeData", menuName = "Items/Tools/AxeData")]
public class AxeData : ToolData
{
    [Header("Specific Settings")]
    [SerializeField] private AAction _actionPrefab;
    [SerializeField] private LayerMask _targetLayers;
    [SerializeField] private int _damage = 1;
    [SerializeField] private float _actionDuration = 0.3f;
    [SerializeField] private float _spawnOffset = 0.5f;

    /// <summary>
    /// Utilise la hache : déclenche l'animation puis instancie la hitbox devant le joueur.
    /// </summary>
    public override bool Use(PlayerController player, Vector2 targetPosition)
    {
        if (player.Actions == null || _actionPrefab == null) return false;

        if (!string.IsNullOrEmpty(_animationTriggerName))
            player.TriggerAnimation(_animationTriggerName);

        player.Actions.SpawnAction(_actionPrefab, _damage, _targetLayers, _actionDuration, _spawnOffset);
        return true;
    }
}
