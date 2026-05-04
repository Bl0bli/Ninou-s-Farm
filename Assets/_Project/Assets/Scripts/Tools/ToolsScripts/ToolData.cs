using UnityEngine;
public abstract class ToolData : ItemData, IUsable
{
    [Header("Tool Settings")]
    public float StaminaCost;
    public float Cooldown;

    /// <summary>
    /// Utilise l'outil sur une position cible donnée.
    /// </summary>
    /// <param name="player">Le contrôleur du joueur utilisant l'outil.</param>
    /// <param name="targetPosition">La position cible de l'utilisation.</param>
    /// <returns>Retourne true si l'outil a été utilisé avec succès, sinon false.</returns>
    public abstract bool Use(PlayerController player, Vector2 targetPosition);
}
