/// <summary>
/// Un emplacement de stockage : vide, ou une pile d'un seul objet (port direct de StorageSlot.cs).
/// C'est une struct : une copie modifiée ne change pas le stockage, seul Storage écrit dans ses slots.
/// </summary>
public struct StorageSlot
{
    public ItemData Item;
    public int Quantity;

    public bool IsEmpty => Item == null || Quantity <= 0;

    /// <summary>La pile a atteint la taille maximale de son objet.</summary>
    public bool IsFull => !IsEmpty && Quantity >= Item.MaxStack;

    public void Clear()
    {
        Item = null;
        Quantity = 0;
    }
}
