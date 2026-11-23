using Godot;

/// <summary>
/// Interface du coffre : la même grille que l'inventaire (UIStorage), sans toolbar, centrée à l'écran (port d'UIChest.cs).
/// Une seule interface sert tous les coffres : elle écoute les événements statiques de Chest et se relie au coffre ouvert.
///
/// Ouverture « parchemin » : le cadre 9-slice (NinePatchRect) grandit en hauteur depuis ses bordures jointes,
/// la grille est dévoilée depuis le milieu (conteneur qui coupe son contenu), et toute la fenêtre rebondit.
/// Dans Unity il fallait un masque et une copie du cadre ; ici le NinePatchRect garde ses bordures à toute taille.
/// </summary>
public partial class UIChest : UIStorage
{
    [ExportGroup("Chest")]
    /// <summary>Cadre du coffre (centré). Masqué tant qu'aucun coffre n'est ouvert.</summary>
    [Export] private NinePatchRect _panel;

    [ExportGroup("Unroll Animation")]
    [Export] private float _openDuration = 0.22f;
    [Export] private float _closeDuration = 0.16f;
    /// <summary>Échelle de départ de l'ouverture et d'arrivée de la fermeture.</summary>
    [Export(PropertyHint.Range, "0.5,1")] private float _closedScale = 0.82f;

    private static UIChest _instance;
    private Chest _openChest;
    private float _panelWidth;
    private float _panelHeight;
    private float _closedHeight;
    private Tween _tween;

    public override void _Ready()
    {
        // Une seule interface de coffre : une copie en trop se retire.
        if (_instance != null && _instance != this)
        {
            QueueFree();
            return;
        }
        _instance = this;

        base._Ready();
        Chest.OnChestOpened += ShowChest;
        Chest.OnChestClosed += HideChest;

        // Taille ouverte = taille du cadre dans la scène. Pas de custom_minimum_size : un Control ne peut pas
        // devenir plus petit que son minimum, le cadre ne pourrait pas se replier.
        _panelWidth = _panel.Size.X;
        _panelHeight = _panel.Size.Y;
        _panel.CustomMinimumSize = Vector2.Zero;
        _closedHeight = _panel.PatchMarginTop + _panel.PatchMarginBottom; // cadre fermé : bordures haute et basse jointes
        _panel.Visible = false;
    }

    public override void _ExitTree()
    {
        base._ExitTree();
        if (_instance != this) return;
        _instance = null;
        Chest.OnChestOpened -= ShowChest;
        Chest.OnChestClosed -= HideChest;
    }

    private void ShowChest(Chest chest)
    {
        _openChest = chest;
        Bind(chest);
        PlayUnroll(true);
    }

    private void HideChest(Chest chest)
    {
        // Seul le coffre affiché peut fermer l'interface.
        if (chest != _openChest) return;

        _openChest = null;
        Bind(null);
        PlayUnroll(false);
    }

    private void PlayUnroll(bool open)
    {
        _tween?.Kill();
        if (open)
        {
            _panel.Visible = true;
            if (_panel.Size.Y < _closedHeight || _panel.Scale.X > 0.99f)
            {
                SetPanelHeight(_closedHeight);
                _panel.Scale = Vector2.One * _closedScale;
            }
        }

        float duration = open ? _openDuration : _closeDuration;
        _tween = CreateTween().SetParallel();
        _tween.TweenMethod(Callable.From<float>(h => SetPanelHeight(Snap(h))), _panel.Size.Y,
                open ? _panelHeight : _closedHeight, duration)
            .SetTrans(Tween.TransitionType.Cubic).SetEase(open ? Tween.EaseType.Out : Tween.EaseType.In);
        _tween.TweenProperty(_panel, "scale", Vector2.One * (open ? 1f : _closedScale), duration)
            .SetTrans(Tween.TransitionType.Back).SetEase(open ? Tween.EaseType.Out : Tween.EaseType.In);

        if (!open) _tween.Chain().TweenCallback(Callable.From(() =>
        {
            _panel.Visible = false;
            _panel.Scale = Vector2.One;
        }));
    }

    /// <summary>Le cadre grandit autour du centre de l'écran : on le recentre à chaque changement de hauteur.</summary>
    private void SetPanelHeight(float height)
    {
        _panel.Size = new Vector2(_panelWidth, height);
        _panel.Position = new Vector2((Size.X - _panelWidth) / 2f, (Size.Y - height) / 2f);
        _panel.PivotOffset = _panel.Size / 2f;
    }

    // Hauteur arrondie à 2 pixels : bords nets sur la grille du pixel art, moitiés haute et basse identiques.
    private float Snap(float height) => Mathf.Max(_closedHeight, Mathf.Round(height / 2f) * 2f);
}
