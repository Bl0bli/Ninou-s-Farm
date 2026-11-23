using Godot;

/// <summary>
/// Une case d'interface : fond, icône, quantité (port de la struct UISlot + du prefab UISlot + d'UIAnimator).
/// C'est un Button plat : Godot gère le clic, le survol et le « consomme » (le monde ne reçoit pas ce clic).
/// </summary>
public partial class UISlot : Button
{
    [Export] private TextureRect _icon;
    [Export] private Label _quantity;

    [ExportGroup("Animation - New Item")]
    [Export] private float _slotAnimDuration = 0.15f;
    [Export] private float _slotShrinkScale = 0.8f;

    [ExportGroup("Animation - Stacked Item")]
    [Export] private float _textAnimDuration = 0.15f;
    [Export] private float _textPopScale = 1.5f;

    [ExportGroup("Animation - Click (UIAnimator SHRINK)")]
    [Export] private float _clickDuration = 0.15f;
    [Export] private float _clickShrinkScale = 0.8f;

    private Tween _slotTween;
    private Tween _textTween;

    public bool HasIcon => _icon.Texture != null;

    public override void _Ready()
    {
        // Le pivot (centre) sert aux mises à l'échelle ; il suit la taille de la case.
        Resized += () => PivotOffset = Size / 2f;
        _quantity.Resized += () => _quantity.PivotOffset = _quantity.Size / 2f;
        PivotOffset = Size / 2f;
        Pressed += () => Pop(this, _clickShrinkScale, _clickDuration, ref _slotTween);
    }

    public void SetItem(Texture2D icon, int quantity)
    {
        _icon.Texture = icon;
        _quantity.Text = quantity.ToString();
    }

    public void Clear()
    {
        _icon.Texture = null;
        _quantity.Text = "";
    }

    // ------------------------------------------------------------------ Glisser-déposer (ajout du port Godot)
    // Godot a un glisser-déposer intégré aux Control : _GetDragData sur la case de départ,
    // _CanDropData / _DropData sur la case d'arrivée. Aucun gestionnaire global à écrire.

    private UIStorage _view;
    private int _index;

    /// <summary>Relie la case à sa vue et à l'index du stockage qu'elle affiche.</summary>
    public void Bind(UIStorage view, int index)
    {
        _view = view;
        _index = index;
    }

    private Storage BoundStorage => _view?.Storage;

    public override Variant _GetDragData(Vector2 atPosition)
    {
        if (BoundStorage == null || BoundStorage.GetSlot(_index).IsEmpty) return default;

        // L'aperçu est dessiné hors de l'interface agrandie : on lui applique la même échelle.
        var preview = new TextureRect { Texture = _icon.Texture, Scale = GetGlobalTransformWithCanvas().Scale };
        var holder = new Control();
        holder.AddChild(preview);
        preview.Position = -_icon.Texture.GetSize() * preview.Scale / 2f;
        SetDragPreview(holder);
        return this;
    }

    public override bool _CanDropData(Vector2 atPosition, Variant data) =>
        data.AsGodotObject() is UISlot from && from != this && from.BoundStorage != null && BoundStorage != null;

    public override void _DropData(Vector2 atPosition, Variant data)
    {
        var from = (UISlot)data.AsGodotObject();
        from.BoundStorage.MoveTo(from._index, BoundStorage, _index);
    }

    /// <summary>Nouvel objet dans la case : elle rétrécit puis revient avec un rebond.</summary>
    public void AnimateNewItem() => Pop(this, _slotShrinkScale, _slotAnimDuration * 2f, ref _slotTween);

    /// <summary>Pile qui grossit : le nombre « saute ».</summary>
    public void AnimateQuantity() => Pop(_quantity, _textPopScale, _textAnimDuration * 1.5f, ref _textTween);

    /// <summary>Équivalent de la Sequence DOTween : vers l'échelle cible, puis retour à 1 avec rebond (OutBack).</summary>
    private void Pop(Control target, float scale, float duration, ref Tween tween)
    {
        tween?.Kill();
        target.Scale = Vector2.One;
        tween = CreateTween();
        tween.TweenProperty(target, "scale", new Vector2(scale, scale), duration / 2f)
            .SetTrans(Tween.TransitionType.Quad).SetEase(Tween.EaseType.Out);
        tween.TweenProperty(target, "scale", Vector2.One, duration / 2f)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    }
}
