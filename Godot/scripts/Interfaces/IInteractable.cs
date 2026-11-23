/// <summary>
/// Objet avec lequel le joueur interagit (touche E) quand il est à portée (port d'IInteractable).
/// Il lui faut une Area2D sur le calque « interactables » (lui-même ou un enfant) : PlayerInteractor la détecte.
/// </summary>
public interface IInteractable
{
    /// <summary>Le joueur appuie sur la touche d'interaction. Renvoie true si quelque chose s'est passé.</summary>
    bool Interact();

    /// <summary>Le joueur quitte la zone d'interaction (fermer un coffre ouvert, par exemple). Rien par défaut.</summary>
    void EndInteraction() { }
}
