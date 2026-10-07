using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public struct UIInventorySlot
{
    public Image Slot;
    public Image Icon;
    public TextMeshProUGUI Quantity;
}
public class UIInventory : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerInventory _playerInventory;
    [SerializeField] private Transform _inventorySlotParent;
    [SerializeField] private GameObject _inventorySlotPrefab;
    [SerializeField] private Transform _toolbar;
    
    [Header("Params")]
    [SerializeField] private int _startInventorySize = 36;
    
    [Header("Animation Settings - New Item")] 
    [SerializeField] private float _slotAnimDuration = 0.15f;
    [SerializeField] private float _slotShrinkScale = 0.8f;
    [SerializeField] private Ease _slotEaseIn = Ease.OutQuad;
    [SerializeField] private Ease _slotEaseOut = Ease.OutBack;
    
    [Header("Animation Settings - Stacked Item")]
    [SerializeField] private float _textAnimDuration = 0.15f;
    [SerializeField] private float _textPopScale = 1.5f;
    
    private List<UIInventorySlot> _inventorySlots = new List<UIInventorySlot>();

    public event Action<int> OnSelectSlot;

    private void Init()
    {
        for (int i = 0; i < _toolbar.transform.childCount; i++)
        {
            UIInventorySlot slot = new UIInventorySlot();
            slot.Slot = _toolbar.transform.GetChild(i).GetComponent<Image>();
            slot.Quantity = _toolbar.transform.GetChild(i).GetChild(0).GetComponent<TextMeshProUGUI>();
            slot.Icon = _toolbar.transform.GetChild(i).GetChild(1).GetComponent<Image>();
            _inventorySlots.Add(slot);

            // Un clic sur un slot de la toolbar équipe l'objet de l'emplacement correspondant.
            int slotIndex = i; // copie locale : la lambda ne doit pas capturer la variable de boucle
            slot.Slot.GetComponent<Button>().onClick.AddListener(() => OnSelectSlot?.Invoke(slotIndex));
        }
        for (int i = 0; i < 36; i++)
        {
            CreateSlot(i);
        }
        RefreshAllInventory();
    }

    private void OnEnable()
    {
        if (_playerInventory != null)
        {
            _playerInventory.OnSlotChanged += UpdateSingleSlot;
            _playerInventory.OnInit += Init;
        }
    }
    
    private void OnDisable()
    {
        if (_playerInventory != null)
        {
            _playerInventory.OnSlotChanged -= UpdateSingleSlot;
            _playerInventory.OnInit -= Init;
        }
    }

    private void CreateSlot(int id)
    {
        GameObject slot = Instantiate(_inventorySlotPrefab, _inventorySlotParent);
        UIInventorySlot uiSlot = new UIInventorySlot();
        uiSlot.Slot = slot.GetComponent<Image>();
        uiSlot.Icon = slot.transform.GetChild(1).GetComponent<Image>();
        uiSlot.Quantity = slot.transform.GetChild(0).GetComponent<TextMeshProUGUI>();
        
        slot.GetComponent<Button>().onClick.AddListener(() => OnSelectSlot?.Invoke(id));
        _inventorySlots.Add(uiSlot);

    }
    private void UpdateSingleSlot(int index)
    {
        UIInventorySlot uiSlot = GetUISlot(index);
        InventorySlot inventorySlot = _playerInventory.Inventory[index];
        if (inventorySlot.IsEmpty)
        {
            ClearUISlot(uiSlot);
            return;
        }

        bool isNewItem = (uiSlot.Icon.sprite == null);
        
        uiSlot.Icon.sprite = inventorySlot.Item.Icon;
        uiSlot.Icon.color = Color.white;
        uiSlot.Quantity.text = inventorySlot.Quantity.ToString();
        
        if (isNewItem)
        {
            AnimateNewSlot(uiSlot.Slot.rectTransform);
        }
        else
        {
            AnimateQuantityText(uiSlot.Quantity.rectTransform);
        }
    }
    
    private void ClearUISlot(UIInventorySlot uiSlot)
    {
        uiSlot.Icon.sprite = null;
        uiSlot.Icon.color = Color.clear;
        uiSlot.Quantity.text = string.Empty;
    }

    private UIInventorySlot GetUISlot(int index) => _inventorySlots[index];
    
    private void RefreshAllInventory()
    {
        for (int i = 0; i < _playerInventory.Inventory.Length; i++)
        {
            UpdateSingleSlot(i);
        }
    }
    
    private void AnimateNewSlot(RectTransform target)
    {
        target.DOKill();
        target.localScale = Vector3.one;
        Sequence anim = DOTween.Sequence();
        
        anim.Append(target.DOScale(new Vector3(_slotShrinkScale, _slotShrinkScale, 1), _slotAnimDuration)
            .SetEase(_slotEaseIn));

        anim.Append(target.DOScale(Vector3.one, _slotAnimDuration)
            .SetEase(_slotEaseOut));
    }

    private void AnimateQuantityText(RectTransform target)
    {
        target.DOKill();
        target.localScale = Vector3.one;

        Sequence anim = DOTween.Sequence();
        
        anim.Append(target.DOScale(new Vector3(_textPopScale, _textPopScale, 1), _textAnimDuration / 2f)
            .SetEase(Ease.OutQuad));

        anim.Append(target.DOScale(Vector3.one, _textAnimDuration)
            .SetEase(Ease.OutBack));
    }
}
