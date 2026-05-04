using UnityEngine;

public interface ICollectible
{
    /// <summary>
    /// Définit le comportement de collecte de l'objet par un inventaire.
    /// </summary>
    /// <param name="inventory">L'inventaire collectant l'objet.</param>
    void Collect(PlayerInventory inventory);
}
