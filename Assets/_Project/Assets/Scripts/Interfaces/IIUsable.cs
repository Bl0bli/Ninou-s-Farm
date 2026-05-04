using UnityEngine;

public interface IUsable
{
    /// <summary>
    /// Définit l'action d'utilisation de l'objet sur une position cible.
    /// </summary>
    /// <param name="player">Le contrôleur du joueur utilisant l'objet.</param>
    /// <param name="targetPosition">La position cible de l'utilisation.</param>
    /// <returns>Retourne true si l'objet a pu être utilisé, sinon false.</returns>
    bool Use(PlayerController player, Vector2 targetPosition);
}
