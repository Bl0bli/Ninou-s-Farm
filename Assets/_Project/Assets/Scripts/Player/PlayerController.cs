using System;
using UnityEngine;
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

    public PlayerActions Actions => _actions;

    [Header("State")] 
    private ItemData _currentItem;
    private int _currentSlotIndex = 0;

    private bool _inventoryToggle = false;
    private bool _isInUI = false;
    
    public event Action<int> OnSlotSelected;

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
        if (_isInUI) return; 

        if (!context.performed) return;

        Vector2 scroll = context.ReadValue<Vector2>();
        if (scroll.y == 0) return;

        int newIndex = _currentSlotIndex;
        
        if (scroll.y > 0)
            newIndex--;
        else if (scroll.y < 0)
            newIndex++;
        
        int maxSlots = _inventory.Inventory.Length; 
        
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
            if (_currentItem != null && _currentItem is IUsable usable)
            {
                ItemData usedItem = _currentItem;
                bool used = usable.Use(this, (Vector2)transform.position + _movements.LastDirection / 2/*GridWorld.Instance.WorldToGridPos((Vector2)transform.position + Vector2.up)*/);

                // Retrait après l'utilisation, sur l'objet utilisé : seul un usage réussi consomme.
                if (used && usedItem.IsConsumable) _inventory.Remove(_currentSlotIndex);
            }
        }
    }

	public void TriggerAnimation(string triggerName) 
	{ 
		_animator.SetTrigger(triggerName);
	}
    
    private void SelectSlot(int index)
    {
        if (_inventory == null) return;
        if (_inventory.Inventory == null || _inventory.Inventory.Length == 0) return;
        if (index < 0 || index >= _inventory.Inventory.Length) return;
        
        _currentSlotIndex = index;

        CheckSlot();

        OnSlotSelected?.Invoke(_currentSlotIndex);
    }

    private void CheckSlot()
    {
        InventorySlot selectedSlot = _inventory.Inventory[_currentSlotIndex];
        
        _currentItem = selectedSlot.IsEmpty ? null : selectedSlot.Item;

        Debug.Log($"[Toolbar] Case {_currentSlotIndex} sélectionnée. En main : {(_currentItem != null ? _currentItem.Name : "Mains vides")}");
    }

    private void OnSlotChanged(int index)
    {
        if(index == _currentSlotIndex) 
            CheckSlot();
    }
}
