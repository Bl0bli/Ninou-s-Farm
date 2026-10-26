using UnityEngine;

/// <summary>
/// Objet avec lequel le joueur interagit (touche E) quand il est à portée.
/// Il lui faut un Collider2D (solide ou trigger) : PlayerInteractor cherche les colliders autour des pieds du joueur.
/// </summary>
public interface IInteractable
{
    /// <summary>Le joueur appuie sur la touche d'interaction. Renvoie true si quelque chose s'est passé.</summary>
    public bool Interact();

    /// <summary>Le joueur quitte la zone d'interaction (fermer un coffre ouvert, par exemple). Rien par défaut.</summary>
    public void EndInteraction() { }
}
