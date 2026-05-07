using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;

public class ItemPickable : MonoBehaviour, ICollectible
{
    [SerializeField] private SpriteRenderer _renderer;
    [SerializeField] private UnityEvent OnPickup;
    [SerializeField] private ItemData _itemData;

    [Header("Idle Floating Animation")]
    [SerializeField] private float _floatHeight = 0.15f;
    [SerializeField] private float _floatDuration = 1.2f;

    private Tween _idleTween;
    private Vector3 _initialLocalPosition;

    private void Start()
    {
        SetupItemPickable();
        StartIdleFloatingAnimation();
    }

    /// <summary>
    /// Initialise l'objet ramassable avec les données d'item spécifiées.
    /// </summary>
    /// <param name="item">Les données de l'item à associer.</param>
    public void Init(ItemData item)
    {
        _itemData = item;
        SetupItemPickable();
    }

    private void SetupItemPickable()
    {
        if (_itemData != null)
        {
            if (_renderer == null)
                _renderer = GetComponent<SpriteRenderer>();

            _renderer.sprite = _itemData.WorldSprite;
        }
    }

    private void StartIdleFloatingAnimation()
    {
        _initialLocalPosition = transform.localPosition;

        _idleTween?.Kill();

        _idleTween = transform
            .DOLocalMoveY(_initialLocalPosition.y + _floatHeight, _floatDuration)
            .SetEase(Ease.InOutSine)
            .SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);
    }

    /// <summary>
    /// Collecte l'objet et tente de l'ajouter à l'inventaire du joueur.
    /// </summary>
    /// <param name="inventory">L'inventaire dans lequel stocker l'objet.</param>
    public void Collect(PlayerInventory inventory)
    {
        if (inventory.PickUp(_itemData))
        {
            OnPickup?.Invoke();

            _idleTween?.Kill();
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        _idleTween?.Kill();
    }
}