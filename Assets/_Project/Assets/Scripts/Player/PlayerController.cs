using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("References")]
	[SerializeField] private Animator _animator;
    [SerializeField] private GameObject _uiInventory;
    [SerializeField] private PlayerInventory _inventory;
    [SerializeField] private UIInventory _uiInventoryView;
    [SerializeField] private PlayerMovements _movements;
    [SerializeField] private PlayerActions _actions;

    [Header("Targeting")]
    [Tooltip("Distance de la cible devant les pieds du joueur, en cases.")]
    [SerializeField] private float _targetDistance = 1f;

    [Header("Interaction")]
    [Tooltip("Sprite affiché au-dessus du personnage quand un objet est à portée (future touche E). Vide : une touche dessinée par défaut.")]
    [SerializeField] private Sprite _interactPromptSprite;
    [SerializeField] private Vector2 _interactPromptOffset = new Vector2(0f, 1.1f);
    [Tooltip("Layer du collider du joueur qui sert de zone d'interaction : un objet interactif qui le chevauche est à portée.")]
    [SerializeField] private string _interactHitboxLayer = "IntaractHitBox";

    public PlayerActions Actions => _actions;
    public PlayerFishing Fishing { get; private set; }
    public PlayerInteractor Interactor { get; private set; }

    /// <summary>SpriteRenderer du personnage (porté par l'objet de l'Animator) : position et tri des effets calés sur le sprite.</summary>
    public SpriteRenderer Visual => _animator.GetComponent<SpriteRenderer>();

    /// <summary>Centre du collider du corps (les pieds), ou la position du joueur à défaut.</summary>
    public Vector2 FeetPosition => _bodyCollider != null ? (Vector2)_bodyCollider.bounds.center : (Vector2)transform.position;

    /// <summary>Dernière direction regardée par le joueur.</summary>
    public Vector2 FacingDirection => _movements.LastDirection;

    [Header("State")] 
    private ItemData _currentItem;
    private int _currentSlotIndex = 0;

    private bool _inventoryToggle = false;
    private BoxCollider2D _bodyCollider;
    private static readonly List<RaycastResult> UIRaycastResults = new List<RaycastResult>();
    
    public event Action<int> OnSlotSelected;

    private void Awake()
    {
        // Ajouté par code : aucun câblage à faire dans l'Inspector ni dans le prefab.
        Fishing = GetComponent<PlayerFishing>();
        if (Fishing == null) Fishing = gameObject.AddComponent<PlayerFishing>();
        Fishing.Init(_animator, _movements);

        Interactor = GetComponent<PlayerInteractor>();
        if (Interactor == null) Interactor = gameObject.AddComponent<PlayerInteractor>();
        Interactor.Init(this, _interactPromptSprite, _interactPromptOffset, _interactHitboxLayer);

        // Collider du corps, porté par la racine du joueur : il est aux pieds du personnage.
        _bodyCollider = GetComponentInParent<BoxCollider2D>();
    }

    private void Start()
    {
        _inventory.OnSlotChanged += OnSlotChanged;
        if (_uiInventoryView != null) _uiInventoryView.OnSelectSlot += SelectSlot;
        SelectSlot(0);
    }

    private void OnDestroy()
    {
        if (_inventory != null) _inventory.OnSlotChanged -= OnSlotChanged;
        if (_uiInventoryView != null) _uiInventoryView.OnSelectSlot -= SelectSlot;
    }

    /// <summary>
    /// Alterne l'affichage de l'interface utilisateur.
    /// </summary>
    public void ToogleInventory()
    {
        _inventoryToggle = !_inventoryToggle;
        _uiInventory.SetActive(_inventoryToggle);
    }
    
    /// <summary>
    /// Gère le changement d'emplacement sélectionné dans l'inventaire via la molette de la souris.
    /// </summary>
    /// <param name="context">Le contexte de l'action contenant les données de défilement.</param>
    public void HandleScrollWheel(InputAction.CallbackContext context)
    {
        if (!context.performed) return;

        Vector2 scroll = context.ReadValue<Vector2>();
        if (scroll.y == 0) return;

        int newIndex = _currentSlotIndex;
        
        if (scroll.y > 0)
            newIndex--;
        else if (scroll.y < 0)
            newIndex++;
        
        int maxSlots = _inventory.ToolbarSize; // la molette ne parcourt que la toolbar 
        
        if (newIndex < 0) 
            newIndex = maxSlots - 1;
        else if (newIndex >= maxSlots) 
            newIndex = 0;

        SelectSlot(newIndex);
    }

    /// <summary>
    /// Utilise l'objet actuellement tenu dans la main du joueur.
    /// </summary>
    /// <param name="context">Le contexte de l'action de déclenchement.</param>
    public void Use(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            if (IsPointerOverUI()) return;
            if (Fishing.IsActive)
            {
                Fishing.Confirm();
                return;
            }

            if (_currentItem != null && _currentItem is IUsable usable)
            {
                ItemData usedItem = _currentItem;
                bool used = usable.Use(this, GetTargetPosition());

                if (used && usedItem.IsConsumable) _inventory.Remove(_currentSlotIndex);
            }
        }
    }

    /// <summary>
    /// Position visée par les outils : devant les pieds du joueur, dans la dernière direction regardée.
    /// Calculée depuis le collider et non depuis transform.position : la racine du prefab est décalée du corps
    /// (sprites à +0,42 en X, collider à -0,43 en Y), ce qui faussait la portée selon la direction.
    /// </summary>
    private Vector2 GetTargetPosition()
    {
        Vector2 origin = _bodyCollider != null ? (Vector2)_bodyCollider.bounds.center : (Vector2)transform.position;
        return origin + _movements.LastDirection.normalized * _targetDistance;
    }

    /// <summary>
    /// Indique si le pointeur est au-dessus d'un élément d'interface (toolbar, inventaire...).
    /// Lancer le rayon à la demande évite IsPointerOverGameObject, qui affiche un warning
    /// quand il est appelé depuis un callback de l'Input System.
    /// </summary>
    private static bool IsPointerOverUI()
    {
        if (EventSystem.current == null || Pointer.current == null) return false;

        PointerEventData pointer = new PointerEventData(EventSystem.current) { position = Pointer.current.position.ReadValue() };
        UIRaycastResults.Clear();
        EventSystem.current.RaycastAll(pointer, UIRaycastResults);
        return UIRaycastResults.Count > 0;
    }

	public void TriggerAnimation(string triggerName) 
	{ 
		_animator.SetTrigger(triggerName);
	}
    
    private void SelectSlot(int index)
    {
        if (_inventory == null) return;
        // Seuls les slots de la toolbar s'équipent ; ceux de la grille servent au rangement.
        if (!_inventory.IsValidIndex(index) || index >= _inventory.ToolbarSize) return;
        
        _currentSlotIndex = index;

        CheckSlot();

        OnSlotSelected?.Invoke(_currentSlotIndex);
    }

    private void CheckSlot()
    {
        StorageSlot selectedSlot = _inventory.GetSlot(_currentSlotIndex);
        
        _currentItem = selectedSlot.IsEmpty ? null : selectedSlot.Item;

        Debug.Log($"[Toolbar] Case {_currentSlotIndex} sélectionnée. En main : {(_currentItem != null ? _currentItem.Name : "Mains vides")}");
    }

    private void OnSlotChanged(int index)
    {
        if(index == _currentSlotIndex) 
            CheckSlot();
    }

    public void SetParameterBool(string isFishingParamName, bool b)
    {
        _animator.SetBool(isFishingParamName, b);
    }
}
