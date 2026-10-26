using UnityEngine;

/// <summary>
/// Inventaire du joueur : la grille de UIStorage, précédée des slots de la toolbar.
/// La toolbar occupe les premiers index du stockage, la grille les suivants.
/// </summary>
public class UIInventory : UIStorage
{
    [Header("Toolbar")]
    [SerializeField] private Transform _toolbar;

    [Header("Params")]
    [Tooltip("Nombre de slots de la grille d'inventaire, en plus de ceux de la toolbar.")]
    [SerializeField] private int _startInventorySize = 36;

    /// <summary>Slots de la toolbar : ils correspondent aux premiers index du stockage.</summary>
    public int ToolbarSize => _toolbar.childCount;

    /// <summary>Nombre total de slots affichés (toolbar + grille) : la taille que doit avoir le stockage.</summary>
    public int SlotCount => ToolbarSize + _startInventorySize;

    protected override void BuildSlots()
    {
        // Slots de la toolbar : déjà présents dans la scène, ils sont seulement enregistrés.
        for (int i = 0; i < ToolbarSize; i++) RegisterSlot(_toolbar.GetChild(i), i);

        // Slots de la grille : créés à la suite.
        for (int i = 0; i < _startInventorySize; i++) CreateGridSlot(ToolbarSize + i);
    }
}
