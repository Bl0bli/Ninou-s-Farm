using UnityEngine;

/// <summary>
/// Inventaire du joueur : un Storage, plus les objets de départ et le ramassage au sol.
/// Sa taille vient de l'interface : autant de slots que la toolbar et la grille en affichent.
/// </summary>
public class PlayerInventory : Storage
{
    [SerializeField] private ItemData[] _startingItems;

    [Tooltip("Interface qui fixe la taille de l'inventaire (toolbar + grille). Cherchée dans le joueur si vide.")]
    [SerializeField] private UIInventory _inventoryView;

    /// <summary>Nombre de slots de la toolbar (les premiers index) : les seuls que le joueur peut équiper.</summary>
    public int ToolbarSize { get; private set; }

    protected override void Start()
    {
        if (_inventoryView == null) _inventoryView = transform.root.GetComponentInChildren<UIInventory>(true);

        if (_inventoryView != null)
            Initialize(_inventoryView.SlotCount);
        else
            Debug.LogWarning("[PlayerInventory] Aucun UIInventory trouvé : la capacité de l'Inspector est utilisée.");

        base.Start(); // n'initialise qu'à défaut, avec la capacité de l'Inspector
        ToolbarSize = _inventoryView != null ? Mathf.Min(_inventoryView.ToolbarSize, Capacity) : Capacity;

        foreach (ItemData item in _startingItems)
        {
            if (item != null) PickUp(item);
        }
    }

    /// <summary>
    /// Récupère un objet et tente de le stocker dans l'inventaire.
    /// Tout ou rien : si la quantité ne tient pas entièrement, rien n'est ajouté
    /// (sinon l'objet au sol resterait entier alors qu'une partie serait déjà rangée).
    /// </summary>
    /// <param name="item">Les données de l'objet à ajouter.</param>
    /// <returns>Retourne true si l'objet a pu être stocké, sinon false.</returns>
    public bool PickUp(ItemData item)
    {
        if (item == null) return false;

        int quantity = Mathf.Max(1, item.Amount);
        if (!CanAdd(item, quantity))
        {
            Debug.Log($"[PlayerInventory] Plus de place pour {item.Name}");
            return false;
        }

        Add(item, quantity);
        return true;
    }
}
