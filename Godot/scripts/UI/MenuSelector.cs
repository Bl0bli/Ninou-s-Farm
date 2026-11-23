using Godot;

/// <summary>
/// Cadre de sélection (quatre coins) qui saute sur la case choisie avec un rebond (port de MenuSelector.cs).
/// </summary>
public partial class MenuSelector : Control
{
    [ExportGroup("Animation Settings")]
    [Export] private float _duration = 0.15f;
    [Export] private float _shrinkScale = 0.8f;

    private Tween _tween;

    public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;

    /// <summary>Se place sur la case (devient son enfant, comme SetParent dans Unity) puis rebondit.</summary>
    public void AnimateSelection(Control slot)
    {
        if (slot == null) return;

        _tween?.Kill();
        if (GetParent() != slot)
        {
            if (GetParent() != null) Reparent(slot, false);
            else slot.AddChild(this);
        }
        SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        PivotOffset = slot.Size / 2f;
        Scale = Vector2.One;

        // Ease.InBounce puis Ease.OutBounce dans Unity.
        _tween = CreateTween();
        _tween.TweenProperty(this, "scale", new Vector2(_shrinkScale, _shrinkScale), _duration)
            .SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.In);
        _tween.TweenProperty(this, "scale", Vector2.One, _duration)
            .SetTrans(Tween.TransitionType.Bounce).SetEase(Tween.EaseType.Out);
    }
}
