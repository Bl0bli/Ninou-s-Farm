using Godot;

/// <summary>
/// Objet tombé au sol, ramassé quand le collecteur du joueur le touche (port d'ItemPickable.cs).
/// DOTween (DOLocalMoveY en yoyo) → Tween Godot en boucle.
/// </summary>
public partial class ItemPickable : Area2D, ICollectible
{
    [Export] private Sprite2D _sprite;
    [Export] private ItemData _itemData;

    [ExportGroup("Idle Floating Animation")]
    [Export] private float _floatHeight = 0.15f;   // en cases, comme dans Unity
    [Export] private float _floatDuration = 1.2f;

    /// <summary>Déclenché au ramassage (UnityEvent OnPickup → signal Godot, branchable dans l'éditeur).</summary>
    [Signal] public delegate void PickedUpEventHandler();

    public ItemData Item => _itemData;

    public void Init(ItemData item)
    {
        _itemData = item;
        if (IsInsideTree()) SetupSprite();
    }

    public override void _Ready()
    {
        SetupSprite();
        StartIdleFloatingAnimation();
    }

    private void SetupSprite()
    {
        if (_itemData != null && _sprite != null) _sprite.Texture = _itemData.WorldSprite;
    }

    private void StartIdleFloatingAnimation()
    {
        float y = _sprite.Position.Y;
        float height = _floatHeight * PlayerMovements.PixelsPerUnit;

        // Le tween appartient au nœud (CreateTween) : il s'arrête tout seul quand le nœud est libéré (SetLink Unity).
        Tween tween = CreateTween().SetLoops().SetTrans(Tween.TransitionType.Sine).SetEase(Tween.EaseType.InOut);
        tween.TweenProperty(_sprite, "position:y", y - height, _floatDuration);
        tween.TweenProperty(_sprite, "position:y", y, _floatDuration);
    }

    public void Collect(PlayerInventory inventory)
    {
        if (!inventory.PickUp(_itemData)) return;

        EmitSignal(SignalName.PickedUp);
        QueueFree();
    }
}
