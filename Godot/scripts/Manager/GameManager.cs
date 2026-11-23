using Godot;

/// <summary>
/// Place le joueur au point d'apparition quand le monde est prêt, et retire l'écran de chargement
/// (port de GameManager.cs + de l'UnityEvent _onWorldInit de GridWorld qui désactivait Canvas_Loading).
/// </summary>
public partial class GameManager : Node
{
    [Export] private Node2D _player;
    [Export] private Control _loadingScreen;
    [Export] private Label _loadingLabel;
    [Export] private ProgressBar _loadingBar;

    public static GameManager Instance { get; private set; }

    public override void _EnterTree()
    {
        if (Instance != null && Instance != this) QueueFree();
        else Instance = this;
    }

    public override void _ExitTree()
    {
        if (Instance == this) Instance = null;
    }

    public override void _Ready()
    {
        GridWorld world = GridWorld.Instance;
        if (world == null) return;

        // Pendant la génération le joueur est figé et caché (il serait sinon dans l'eau, en (0, 0)).
        _player.ProcessMode = ProcessModeEnum.Disabled;
        _player.Visible = false;

        world.OnProgress += (progress, step) =>
        {
            if (_loadingBar != null) _loadingBar.Value = progress * 100f;
            if (_loadingLabel != null) _loadingLabel.Text = $"Génération du monde… {step}  (Espace : passer)";
        };
        world.OnWorldInit += SpawnPlayer;
    }

    private void SpawnPlayer(Vector2 position)
    {
        _player.GlobalPosition = position;
        _player.ProcessMode = ProcessModeEnum.Inherit;
        _player.Visible = true;
        if (_loadingScreen != null) _loadingScreen.Visible = false;
        if (_player.GetNodeOrNull<Camera2D>("Camera2D") is Camera2D camera) camera.ResetSmoothing();
    }
}
