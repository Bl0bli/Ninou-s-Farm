using UnityEngine;

public interface IDamageable
{
    /// <summary>
    /// Applique un montant de dégâts à l'objet.
    /// </summary>
    /// <param name="amount">Le montant de dégâts à infliger.</param>
    void TakeDamage(int amount);
}
