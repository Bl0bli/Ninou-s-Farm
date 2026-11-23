using Godot;

/// <summary>
/// Orchestre une session de pêche : attente de la morsure, jauge à valider, résultat (port de PlayerFishing.cs).
/// Phases : Waiting (lancer + attente) → Biting (jauge) → Ending (animation de fin) → None.
///
/// Unity pilotait l'Animator (FISH, FishBite, FishCaught, IsFishing) et attendait la sortie des états « Fishing ».
/// Ici on prend la main sur le sprite (PlayerMovements.BeginExternalControl) et on enchaîne les animations
/// nous-mêmes : fishing_start → fishing_idle → fishing_struggle → show (pris) ou fishing_quit (raté).
/// </summary>
public partial class PlayerFishing : Node
{
    private enum Phase { None, Waiting, Biting, Ending }

    private const string PickableScenePath = "res://scenes/item_pickable.tscn";
    private const float EndingTimeout = 8f;

    [Export] private PlayerMovements _movements;

    private UIFishingGauge _ui;
    private Phase _phase = Phase.None;
    private FishingRodData _rod;
    private Vector2I _waterCell;
    private FishData _fish;
    private FishingGauge _gauge;
    private float _timer;

    /// <summary>Vrai tant qu'une pêche est en cours : le clic est alors dirigé vers Confirm().</summary>
    public bool IsActive => _phase != Phase.None;

    public override void _Ready()
    {
        // Comme Unity (new GameObject + AddComponent) : l'interface est créée par code, rien à câbler.
        _ui = new UIFishingGauge { Name = "UIFishingGauge" };
        AddChild(_ui);
        _movements.ExternalAnimationFinished += OnAnimationFinished;
    }

    /// <summary>Démarre une pêche vers la case d'eau visée. False si une pêche est déjà en cours.</summary>
    public bool TryBegin(FishingRodData rod, Vector2I waterCell)
    {
        if (IsActive) return false;

        _rod = rod;
        _waterCell = waterCell;

        _movements.BeginExternalControl();
        _movements.PlayDirectional("fishing_start");

        _timer = rod.RollBiteDelay();
        _phase = Phase.Waiting;
        return true;
    }

    /// <summary>
    /// Clic du joueur pendant la pêche : pendant l'attente, il remonte la ligne (rien ne sort) ;
    /// pendant la morsure, il valide la jauge.
    /// </summary>
    public void Confirm()
    {
        switch (_phase)
        {
            case Phase.Waiting:
                Finish(false, showGauge: false);
                break;
            case Phase.Biting:
                Finish(_gauge.Confirm(), showGauge: true);
                break;
        }
    }

    public override void _Process(double delta)
    {
        float dt = (float)delta;
        switch (_phase)
        {
            case Phase.Waiting:
                _timer -= dt;
                if (_timer <= 0f) StartBite();
                break;

            case Phase.Biting:
                _gauge.Tick(dt);
                _timer -= dt;
                if (_timer <= 0f)
                {
                    _gauge.Stop();
                    Finish(false, showGauge: true);
                }
                break;

            case Phase.Ending:
                // Filet de sécurité : si l'animation de fin ne se termine jamais, on rend quand même la main.
                _timer -= dt;
                if (_timer <= 0f) EndSession();
                break;
        }
    }

    /// <summary>Enchaînements de l'Animator : fin du lancer → attente ; fin de « show » / « quit » → fin de session.</summary>
    private void OnAnimationFinished(string anim)
    {
        if (anim.StartsWith("fishing_start") && _phase == Phase.Waiting) _movements.PlayDirectional("fishing_idle");
        else if (_phase == Phase.Ending && (anim.StartsWith("show") || anim.StartsWith("fishing_quit"))) EndSession();
    }

    private void StartBite()
    {
        _fish = _rod.LootTable?.Roll();
        if (_fish == null)
        {
            GD.PushWarning("[PlayerFishing] Table de poissons vide : la pêche est annulée.");
            Finish(false, showGauge: false);
            return;
        }

        // Zone placée au hasard ; sa largeur et la vitesse du curseur dépendent du poisson tiré.
        float half = _fish.ZoneWidth / 2f;
        float center = (float)GD.RandRange(half, 1f - half);
        _gauge = new FishingGauge(center, _fish.ZoneWidth, _fish.CursorSpeed);
        _ui.Bind(_gauge);

        _movements.PlayDirectional("fishing_struggle");
        _timer = _rod.HitWindow;
        _phase = Phase.Biting;
    }

    private void Finish(bool success, bool showGauge)
    {
        if (success)
        {
            _movements.PlayDirectional("show");
            DropFish();
        }
        else
        {
            _movements.PlayDirectional("fishing_quit");
        }

        if (showGauge) _ui.ShowResult(success);
        else _ui.HideGauge();

        _timer = EndingTimeout;
        _phase = Phase.Ending;
    }

    /// <summary>Le poisson tombe sur la case d'eau ; l'aimant du joueur le ramène.</summary>
    private void DropFish()
    {
        var pickable = GD.Load<PackedScene>(PickableScenePath).Instantiate<ItemPickable>();
        pickable.Init(_fish);

        ITileGrid grid = TileGridLocator.Current;
        Vector2 position = grid != null ? grid.GridToWorld(_waterCell) : _movements.GlobalPosition;
        _movements.GetParent().AddChild(pickable);
        pickable.GlobalPosition = position;
    }

    private void EndSession()
    {
        _movements.EndExternalControl();
        _gauge = null;
        _fish = null;
        _phase = Phase.None;
    }

    // OnDisable Unity : retiré en pleine pêche, on ne laisse ni le joueur bloqué, ni la jauge affichée.
    public override void _ExitTree()
    {
        _movements.ExternalAnimationFinished -= OnAnimationFinished;
        if (!IsActive) return;
        _ui?.HideGauge();
        _movements.EndExternalControl();
        _phase = Phase.None;
    }
}
