using System;
using Godot;

/// <summary>
/// Widget d'heure (port de TimeUI.cs) : un ciel qui passe du jour au coucher puis à la nuit en fondu,
/// et une flèche qui claque vers l'angle de la tranche horaire, avec un léger dépassement.
/// </summary>
public partial class TimeUI : Control
{
    private enum SkyPhase { Day, Sunset, Night }

    private readonly record struct SkyKeyframe(float Hour, SkyPhase Phase);
    private readonly record struct ArrowKeyframe(float Hour, float Angle);

    [ExportGroup("UI Refs")]
    [Export] private TextureRect _dayImage;
    [Export] private TextureRect _sunsetImage;
    [Export] private TextureRect _nightImage;

    [ExportGroup("Weather Arrow")]
    /// <summary>Le pivot parent de la flèche : c'est lui qui tourne.</summary>
    [Export] private Control _arrowPivot;
    [Export] private float _arrowRotateDuration = 0.35f;

    // Les tableaux de structs [Serializable] d'Unity ne s'exportent pas tels quels en Godot : valeurs reprises du jeu.
    // Heures triées ; le cycle boucle sur 24 h : entre deux phases identiques le ciel est stable, sinon il fond.
    private static readonly SkyKeyframe[] Keyframes =
    {
        new(6f, SkyPhase.Night),
        new(7f, SkyPhase.Day),
        new(16f, SkyPhase.Day),
        new(17f, SkyPhase.Sunset),
        new(20f, SkyPhase.Night),
    };

    /// <summary>Angles en degrés, sens Unity (positif = sens inverse des aiguilles d'une montre).</summary>
    private static readonly ArrowKeyframe[] ArrowKeyframes =
    {
        new(7f, 45f),
        new(17f, 0f),
        new(20f, -45f),
    };

    private Tween _arrowTween;
    private float _arrowAngle;
    private bool _arrowInitialized;

    public override void _Ready()
    {
        TimeManager.OnMinuteChanged += UpdateTime;
        UpdateTime();
    }

    public override void _ExitTree()
    {
        TimeManager.OnMinuteChanged -= UpdateTime;
        _arrowTween?.Kill();
    }

    private void UpdateTime()
    {
        float hour = TimeManager.Hour + TimeManager.Minute / 60f;
        UpdateSky(hour);
        UpdateArrow(hour);
    }

    /// <summary>La flèche ne suit pas l'heure en continu : elle claque vers l'angle de la tranche (TRANS_BACK = OutBack).</summary>
    private void UpdateArrow(float hour)
    {
        if (_arrowPivot == null) return;

        // Godot tourne dans le sens des aiguilles d'une montre (Y vers le bas) : on inverse l'angle Unity.
        float target = Mathf.DegToRad(-GetArrowAngle(hour));

        if (!_arrowInitialized)
        {
            _arrowInitialized = true;
            _arrowAngle = target;
            _arrowPivot.Rotation = target;
            return;
        }

        if (Mathf.IsEqualApprox(target, _arrowAngle)) return;

        _arrowAngle = target;
        _arrowTween?.Kill();
        _arrowTween = CreateTween();
        _arrowTween.TweenProperty(_arrowPivot, "rotation", target, _arrowRotateDuration)
            .SetTrans(Tween.TransitionType.Back).SetEase(Tween.EaseType.Out);
    }

    /// <summary>Angle de la dernière keyframe atteinte ; avant la première de la journée, celle de la veille.</summary>
    private static float GetArrowAngle(float hour)
    {
        int index = -1;
        for (int i = 0; i < ArrowKeyframes.Length; i++)
            if (ArrowKeyframes[i].Hour <= hour) index = i;

        if (index < 0) index = ArrowKeyframes.Length - 1;
        return ArrowKeyframes[index].Angle;
    }

    private void UpdateSky(float hour)
    {
        int next = 0;
        while (next < Keyframes.Length && Keyframes[next].Hour <= hour) next++;

        int fromIndex = (next - 1 + Keyframes.Length) % Keyframes.Length;
        int toIndex = next % Keyframes.Length;

        // PosMod gère le passage de minuit (ex : 21 h -> 6 h = 9 h de segment), comme Mathf.Repeat.
        float segment = Mathf.PosMod(Keyframes[toIndex].Hour - Keyframes[fromIndex].Hour, 24f);
        float elapsed = Mathf.PosMod(hour - Keyframes[fromIndex].Hour, 24f);
        float blend = segment > 0f ? Mathf.Clamp(elapsed / segment, 0f, 1f) : 0f;

        ApplyBlend(Keyframes[fromIndex].Phase, Keyframes[toIndex].Phase, blend);
    }

    /// <summary>
    /// La phase cible apparaît par-dessus la phase courante restée opaque : empiler (au lieu de croiser deux alphas)
    /// évite l'effet « délavé » au milieu du fondu.
    /// </summary>
    private void ApplyBlend(SkyPhase from, SkyPhase to, float blend)
    {
        SetAlpha(_dayImage, 0f);
        SetAlpha(_sunsetImage, 0f);
        SetAlpha(_nightImage, 0f);
        SetAlpha(GetImage(from), 1f);

        if (from == to) return;

        TextureRect toImage = GetImage(to);
        BringToFrontOfBackgrounds(toImage);
        SetAlpha(toImage, blend);
    }

    /// <summary>Place l'image devant les deux autres ciels, sans passer devant le portrait (SetSiblingIndex → MoveChild).</summary>
    private void BringToFrontOfBackgrounds(TextureRect target)
    {
        int top = Math.Max(_dayImage.GetIndex(), Math.Max(_sunsetImage.GetIndex(), _nightImage.GetIndex()));
        if (target.GetIndex() != top) target.GetParent().MoveChild(target, top);
    }

    private TextureRect GetImage(SkyPhase phase) => phase switch
    {
        SkyPhase.Day => _dayImage,
        SkyPhase.Sunset => _sunsetImage,
        _ => _nightImage,
    };

    private static void SetAlpha(CanvasItem image, float alpha)
    {
        if (image == null) return;
        Color c = image.Modulate;
        c.A = alpha;
        image.Modulate = c;
    }
}
