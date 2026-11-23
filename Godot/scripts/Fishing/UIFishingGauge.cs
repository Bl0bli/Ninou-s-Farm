using Godot;

/// <summary>
/// Jauge de pêche verticale, sur le bord droit de l'écran (port d'UIFishingGauge.cs). Aucune logique :
/// elle dessine l'état d'un FishingGauge. Comme dans Unity, elle construit elle-même ses éléments
/// (un CanvasLayer et des ColorRect à la place du Canvas et des Image), donc rien à câbler dans la scène.
/// </summary>
public partial class UIFishingGauge : CanvasLayer
{
    private static readonly Color BorderColor = new(0.93f, 0.80f, 0.60f);
    private static readonly Color BackgroundColor = new(0.27f, 0.17f, 0.14f);
    private static readonly Color ZoneColor = new(0.52f, 0.75f, 0.40f);
    private static readonly Color CursorColor = new(0.96f, 0.96f, 0.90f);
    private static readonly Color HitColor = new(0.55f, 0.90f, 0.45f);
    private static readonly Color MissColor = new(0.85f, 0.30f, 0.25f);

    // Tailles Unity (référence 1920x1080) ramenées à l'écran de base Godot (1280x720) : x 2/3.
    private const float PanelWidth = 37f;
    private const float PanelHeight = 347f;
    private const float Border = 4f;
    private const float RightMargin = 53f;
    private const float CursorHeight = 5f;

    private Control _frame;
    private Control _track;
    private ColorRect _zone;
    private ColorRect _cursor;
    private FishingGauge _gauge;
    private SceneTreeTimer _hideTimer;

    public override void _Ready()
    {
        Layer = 50; // devant le reste de l'interface (sortingOrder 50 dans Unity)
        Build();
    }

    /// <summary>Affiche la jauge et la relie à la logique : le curseur suit OnCursorMoved.</summary>
    public void Bind(FishingGauge gauge)
    {
        Unbind();
        _gauge = gauge;
        _gauge.OnCursorMoved += MoveCursor;

        _hideTimer = null;
        _cursor.Color = CursorColor;
        SetZone(gauge.ZoneMin, gauge.ZoneMax);
        MoveCursor(gauge.Cursor);
        _frame.Visible = true;
    }

    /// <summary>Colore le curseur selon le résultat puis masque la jauge après un court instant.</summary>
    public void ShowResult(bool success, float duration = 0.5f)
    {
        _cursor.Color = success ? HitColor : MissColor;
        SceneTreeTimer timer = GetTree().CreateTimer(duration);
        _hideTimer = timer;
        // Seul le dernier minuteur compte (StopCoroutine dans Unity) : une nouvelle pêche annule ce masquage.
        timer.Timeout += () => { if (_hideTimer == timer) HideGauge(); };
    }

    public void HideGauge()
    {
        _hideTimer = null;
        Unbind();
        _frame.Visible = false;
    }

    public override void _ExitTree() => Unbind();

    private void Unbind()
    {
        if (_gauge != null) _gauge.OnCursorMoved -= MoveCursor;
        _gauge = null;
    }

    /// <summary>0 = bas de la piste, 1 = haut. Les anchors Unity (y vers le haut) deviennent une position y inversée.</summary>
    private void MoveCursor(float value)
    {
        float y = (1f - Mathf.Clamp(value, 0f, 1f)) * _track.Size.Y;
        _cursor.Position = new Vector2((_track.Size.X - _cursor.Size.X) / 2f, y - CursorHeight / 2f);
    }

    private void SetZone(float min, float max)
    {
        float h = _track.Size.Y;
        _zone.Position = new Vector2(0, (1f - max) * h);
        _zone.Size = new Vector2(_track.Size.X, (max - min) * h);
    }

    private void Build()
    {
        // Cadre : ancré au milieu du bord droit.
        _frame = new ColorRect { Name = "Frame", Color = BorderColor, MouseFilter = Control.MouseFilterEnum.Ignore };
        AddChild(_frame);
        // Ancres au milieu du bord droit, puis offsets mesurés depuis ces ancres (anchoredPosition + sizeDelta Unity).
        // Attention : Position, elle, se mesure depuis le coin haut-gauche du parent, pas depuis l'ancre.
        _frame.SetAnchorsPreset(Control.LayoutPreset.CenterRight);
        _frame.OffsetLeft = -RightMargin - PanelWidth;
        _frame.OffsetRight = -RightMargin;
        _frame.OffsetTop = -PanelHeight / 2f;
        _frame.OffsetBottom = PanelHeight / 2f;

        // Piste : l'intérieur du cadre, c'est elle qui définit l'échelle 0 à 1.
        _track = new ColorRect { Name = "Track", Color = BackgroundColor, MouseFilter = Control.MouseFilterEnum.Ignore };
        _frame.AddChild(_track);
        _track.Position = new Vector2(Border, Border);
        _track.Size = new Vector2(PanelWidth - 2 * Border, PanelHeight - 2 * Border);

        _zone = new ColorRect { Name = "Zone", Color = ZoneColor, MouseFilter = Control.MouseFilterEnum.Ignore };
        _track.AddChild(_zone);

        // Curseur : un trait horizontal un peu plus large que la piste.
        _cursor = new ColorRect { Name = "Cursor", Color = CursorColor, MouseFilter = Control.MouseFilterEnum.Ignore };
        _track.AddChild(_cursor);
        _cursor.Size = new Vector2(PanelWidth + 5f, CursorHeight);

        _frame.Visible = false;
    }
}
