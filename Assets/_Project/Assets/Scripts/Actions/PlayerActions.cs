using UnityEngine;

public class PlayerActions : MonoBehaviour
{
    [SerializeField] private PlayerMovements _movements;
    [SerializeField] private float _defaultSpawnOffset = 0.5f;

    /// <summary>
    /// Instancie une hitbox d'action devant le joueur, orientée selon sa dernière direction.
    /// </summary>
    public AAction SpawnAction(AAction prefab, int damage, LayerMask targetLayers, float duration, float spawnOffset = -1f)
    {
        if (prefab == null) return null;

        Vector2 dir = _movements.LastDirection.sqrMagnitude > 0.01f
            ? _movements.LastDirection.normalized
            : Vector2.down;

        float offset = spawnOffset > 0f ? spawnOffset : _defaultSpawnOffset;
        Vector3 spawnPos = transform.position + (Vector3)(dir * offset);

        AAction action = Instantiate(prefab, spawnPos, Quaternion.identity);
        action.Init(damage, targetLayers, dir, duration);
        return action;
    }
}
