using System;
using Godot;

/// <summary>
/// Grille de la scène de test des outils : port de TestTileGrid.cs ET de la peinture de la carte
/// faite par ToolsTestSceneBuilder.cs. Dans Unity, la carte était peinte par un script d'éditeur puis relue ;
/// ici elle est simplement construite au lancement, à partir des mêmes rectangles.
///
/// Carte : un rectangle de terre bordé d'eau, avec un étang et un plateau (falaise).
/// Coordonnées Godot (Y vers le bas) : la carte Unity est retournée verticalement.
/// </summary>
public partial class TestTileGrid : Node2D, ITileGrid
{
    public const int MapWidth = 24;
    public const int MapHeight = 16;
    private const int WaterMargin = 20; // 6 dans Unity : élargi pour que la caméra ne sorte jamais de l'eau

    private static readonly Rect2I Land = new(1, 1, MapWidth - 2, MapHeight - 2);
    private static readonly Rect2I Pond = new(17, 3, 3, 3);
    private static readonly Rect2I Plateau = new(3, 3, 4, 3);

    [Export] private TileMapLayer _waterLayer;
    [Export] private TileMapLayer _groundLayer;
    [Export] private TileMapLayer _floorLayer;
    [Export] private TileMapLayer _collisionLayer;
    [Export] private BiomeType _biome = BiomeType.HILLS;

    private TileGridData _data;

    public event Action<Vector2I> OnTileChanged;

    // _EnterTree des parents passe avant le _Ready des enfants : la grille est enregistrée
    // avant que le joueur ou les outils ne la cherchent (rôle de l'Awake Unity).
    public override void _EnterTree()
    {
        _data = BuildData();
        _data.OnTileChanged += cell => OnTileChanged?.Invoke(cell);
        TileGridLocator.Register(this);
    }

    public override void _ExitTree() => TileGridLocator.Unregister(this);

    public override void _Ready() => PaintMap();

    private static bool IsLand(Vector2I c) => Land.HasPoint(c) && !Pond.HasPoint(c);
    private static bool IsFloor(Vector2I c) => IsLand(c) && Plateau.HasPoint(c);

    private TileGridData BuildData()
    {
        var tiles = new TileData[MapWidth, MapHeight];
        for (int x = 0; x < MapWidth; x++)
        for (int y = 0; y < MapHeight; y++)
        {
            var cell = new Vector2I(x, y);
            TileType type = IsFloor(cell) ? TileType.FLOOR : IsLand(cell) ? TileType.GROUND : TileType.WATER;
            BiomeType biome = type == TileType.WATER ? BiomeType.WATER : _biome;
            tiles[x, y] = new TileData(cell, biome, type);
        }
        return new TileGridData(tiles);
    }

    /// <summary>Comme GridWorld : l'eau est posée partout, le sol la recouvre, la falaise recouvre le sol.</summary>
    private void PaintMap()
    {
        RuleTile water = RuleTile.Get("water");
        RuleTile ground = RuleTile.Get("ground");
        RuleTile floor = RuleTile.Get("floor");
        RuleTile.TileRef blocker = RuleTile.Blocker;

        for (int x = -WaterMargin; x < MapWidth + WaterMargin; x++)
        for (int y = -WaterMargin; y < MapHeight + WaterMargin; y++)
        {
            var cell = new Vector2I(x, y);
            water.Paint(_waterLayer, cell, _ => true);

            if (!IsLand(cell))
            {
                _collisionLayer.SetCell(cell, blocker.Source, blocker.Atlas, blocker.Alternative);
                continue;
            }

            ground.Paint(_groundLayer, cell, IsLand);
            if (IsFloor(cell)) floor.Paint(_floorLayer, cell, IsFloor);
        }
    }

    public Vector2I WorldToGrid(Vector2 worldPosition) => _groundLayer.LocalToMap(_groundLayer.ToLocal(worldPosition));

    public Vector2 GridToWorld(Vector2I gridPosition) => _groundLayer.ToGlobal(_groundLayer.MapToLocal(gridPosition));

    public bool IsOccupied(Vector2I p) => _data.IsOccupied(p);
    public bool TryOccupy(Vector2I p, CropInstance crop) => _data.TryOccupy(p, crop);
    public bool TryGetCrop(Vector2I p, out CropInstance crop) => _data.TryGetCrop(p, out crop);
    public void Release(Vector2I p, CropInstance crop) => _data.Release(p, crop);

    public TileData GetTileAt(Vector2I p) => _data.GetTileAt(p);
    public bool CanBeFarmed(Vector2I p) => _data.CanBeFarmed(p);
    public bool SetTileState(Vector2I p, TileState state) => _data.SetTileState(p, state);
    public bool SetTileWatered(Vector2I p, bool isWatered) => _data.SetTileWatered(p, isWatered);
}
