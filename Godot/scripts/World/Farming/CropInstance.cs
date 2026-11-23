using Godot;

/// <summary>
/// Culture plantée sur une case : pousse d'un stade par heure de jeu arrosée, se récolte à la houe
/// (port de CropInstance.cs).
/// </summary>
public partial class CropInstance : Dropable
{
    [Export] private Sprite2D _sprite;

    private CropData _cropData;
    private ItemData _seed;
    private Vector2I _cell;
    private int _elapsedHours;
    private int _stage = -1;

    public bool IsGrown => _stage >= _cropData.GrowStages.Length - 1;

    /// <param name="seed">Graine plantée, rendue si la culture est arrachée avant maturité.</param>
    public void Init(CropData cropData, Vector2I cell, ItemData seed)
    {
        _cropData = cropData;
        _cell = cell;
        _seed = seed;
        _elapsedHours = 0;
        _itemDropped = _cropData.Loot;
        Name = $"Crop_{cropData.CropName}_{cell.X}_{cell.Y}";
        SetStage(0);
        TimeManager.OnHourChanged += OnHourChanged;
    }

    // OnDestroy Unity → _ExitTree : on se désabonne de l'événement statique, sinon il garderait un nœud libéré.
    public override void _ExitTree()
    {
        TimeManager.OnHourChanged -= OnHourChanged;
        TileGridLocator.Current?.Release(_cell, this);
    }

    /// <summary>Arrache la culture : une plante mûre donne sa récolte, une plante en pousse rend sa graine.</summary>
    public void Harvest()
    {
        _itemDropped = IsGrown ? _cropData.Loot : _seed;

        if (_itemDropped == null) QueueFree();
        else DropItem(true);
    }

    private void OnHourChanged(float _)
    {
        if (!IsWatered()) return;

        _elapsedHours++;
        SetStage(ComputeStage());
    }

    private bool IsWatered()
    {
        ITileGrid grid = TileGridLocator.Current;
        return grid != null && grid.GetTileAt(_cell).IsWatered;
    }

    private int ComputeStage()
    {
        int lastStage = _cropData.GrowStages.Length - 1;
        if (_cropData.GrowTime <= 0) return lastStage;

        float progress = Mathf.Clamp(_elapsedHours / (float)_cropData.GrowTime, 0f, 1f);
        return Mathf.FloorToInt(progress * lastStage);
    }

    private void SetStage(int stage)
    {
        if (stage <= _stage) return;

        _stage = stage;
        _sprite.Texture = _cropData.GrowStages[_stage];

        if (IsGrown)
        {
            TimeManager.OnHourChanged -= OnHourChanged;
            GD.Print($"Growth Completed for {Name}");
        }
    }
}
