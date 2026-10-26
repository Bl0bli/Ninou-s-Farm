using System;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public struct UISlot
{
    public Image Slot;
    public Image Icon;
    public TextMeshProUGUI Quantity;
}

/// <summary>
/// Affichage d'un Storage sous forme de grille de slots, avec les animations d'ajout et d'empilement.
/// Base commune de l'inventaire du joueur (UIInventory, qui ajoute la toolbar) et du coffre (UIChest).
/// Le slot affiché i est toujours le slot i du stockage.
/// </summary>
public class UIStorage : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Stockage affiché. Peut rester vide si la vue est reliée par code (Bind).")]
    [FormerlySerializedAs("_playerInventory")]
    [SerializeField] protected Storage _storage;
    [FormerlySerializedAs("_inventorySlotParent")]
    [SerializeField] protected Transform _slotParent;
    [FormerlySerializedAs("_inventorySlotPrefab")]
    [SerializeField] protected GameObject _slotPrefab;

    [Header("Animation Settings - New Item")]
    [SerializeField] private float _slotAnimDuration = 0.15f;
    [SerializeField] private float _slotShrinkScale = 0.8f;
    [SerializeField] private Ease _slotEaseIn = Ease.OutQuad;
    [SerializeField] private Ease _slotEaseOut = Ease.OutBack;

    [Header("Animation Settings - Stacked Item")]
    [SerializeField] private float _textAnimDuration = 0.15f;
    [SerializeField] private float _textPopScale = 1.5f;

    private readonly List<UISlot> _slots = new List<UISlot>();
    private readonly List<GameObject> _createdSlots = new List<GameObject>();
    private bool _animate;

    /// <summary>Clic sur un slot, avec l'index du stockage correspondant.</summary>
    public event Action<int> OnSelectSlot;

    public Storage Storage => _storage;

    protected virtual void OnEnable() => Subscribe();
    protected virtual void OnDisable() => Unsubscribe();

    /// <summary>
    /// Relie la vue à un autre stockage (un coffre qu'on ouvre, par exemple) et reconstruit la grille.
    /// </summary>
    public void Bind(Storage storage)
    {
        Unsubscribe();
        _storage = storage;
        Subscribe();
        if (_storage != null && _storage.IsInitialized) Rebuild();
    }

    private void Subscribe()
    {
        if (_storage == null) return;
        _storage.OnSlotChanged += UpdateSingleSlot;
        _storage.OnInit += Rebuild;
    }

    private void Unsubscribe()
    {
        if (_storage == null) return;
        _storage.OnSlotChanged -= UpdateSingleSlot;
        _storage.OnInit -= Rebuild;
    }

    /// <summary>
    /// Recrée les slots affichés pour le stockage courant, puis les remplit sans animation.
    /// </summary>
    private void Rebuild()
    {
        foreach (GameObject created in _createdSlots) Destroy(created);
        _createdSlots.Clear();
        _slots.Clear();

        BuildSlots();

        _animate = false;
        for (int i = 0; i < _slots.Count; i++) UpdateSingleSlot(i);
        _animate = true;
    }

    /// <summary>
    /// Crée un slot de grille par slot du stockage. UIInventory la surcharge pour placer la toolbar en premier.
    /// </summary>
    protected virtual void BuildSlots()
    {
        for (int i = 0; i < _storage.Capacity; i++) CreateGridSlot(i);
    }

    /// <summary>
    /// Instancie un slot dans la grille, relié à l'index donné du stockage.
    /// </summary>
    protected void CreateGridSlot(int storageIndex)
    {
        GameObject slot = Instantiate(_slotPrefab, _slotParent);
        _createdSlots.Add(slot);
        RegisterSlot(slot.transform, storageIndex);
    }

    /// <summary>
    /// Ajoute un slot existant (toolbar) ou créé (grille) à la liste affichée, dans l'ordre des index du stockage.
    /// </summary>
    protected void RegisterSlot(Transform slot, int storageIndex)
    {
        UISlot uiSlot = new UISlot
        {
            Slot = slot.GetComponent<Image>(),
            Quantity = slot.GetChild(0).GetComponent<TextMeshProUGUI>(),
            Icon = slot.GetChild(1).GetComponent<Image>(),
        };
        _slots.Add(uiSlot);

        Button button = slot.GetComponent<Button>();
        if (button != null) button.onClick.AddListener(() => OnSelectSlot?.Invoke(storageIndex));
    }

    private void UpdateSingleSlot(int index)
    {
        if (index < 0 || index >= _slots.Count) return;

        UISlot uiSlot = _slots[index];
        StorageSlot storageSlot = _storage.GetSlot(index);
        if (storageSlot.IsEmpty)
        {
            ClearUISlot(uiSlot);
            return;
        }

        bool isNewItem = uiSlot.Icon.sprite == null;

        uiSlot.Icon.sprite = storageSlot.Item.Icon;
        uiSlot.Icon.color = Color.white;
        uiSlot.Quantity.text = storageSlot.Quantity.ToString();

        if (!_animate) return;
        if (isNewItem) AnimateNewSlot(uiSlot.Slot.rectTransform);
        else AnimateQuantityText(uiSlot.Quantity.rectTransform);
    }

    private static void ClearUISlot(UISlot uiSlot)
    {
        uiSlot.Icon.sprite = null;
        uiSlot.Icon.color = Color.clear;
        uiSlot.Quantity.text = string.Empty;
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
