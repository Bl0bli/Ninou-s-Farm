public interface ICollectible
{
    /// <summary>Comportement de collecte de l'objet par un inventaire (port direct d'ICollectible).</summary>
    void Collect(PlayerInventory inventory);
}
