using Godot;

public interface IUsable
{
    /// <summary>
    /// Utilise l'objet sur une position cible (port direct d'IUsable).
    /// </summary>
    /// <param name="player">Le contrôleur du joueur utilisant l'objet.</param>
    /// <param name="targetPosition">Position globale visée, en pixels.</param>
    /// <returns>True si l'objet a pu être utilisé.</returns>
    bool Use(PlayerController player, Vector2 targetPosition);
}
