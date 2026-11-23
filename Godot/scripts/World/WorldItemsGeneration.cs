using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// Décor et objets interactifs du monde (port de WorldItemsGeneration.cs). Chaque case est découpée en 3x3
/// sous-cases ; sur chacune, un objet peut apparaître (taux _gridItemSpawnRate), choisi par le bruit pour former
/// des taches cohérentes, en respectant le biome et sans déborder sur une case différente.
///
/// Dans Unity : une Tilemap de cases de 1/3 d'unité. Ici une case fait 16/3 px (pas un entier), on dessine
/// donc le décor nous-mêmes (DecorLayer, un seul nœud pour des milliers d'images) au lieu d'un TileMapLayer.
/// Les objets interactifs (arbres) sont des scènes instanciées sous _interactiveItemsParent.
/// </summary>
public partial class WorldItemsGeneration : Node
{
    private const string CatalogPath = "res://resources/world/world_items.json";
    private const int Sub = 3; // sous-cases par côté de case

    [Export] private DecorLayer _decor;
    /// <summary>Parent des objets interactifs : le nœud trié en Y du monde (arbres devant / derrière le joueur).</summary>
    [Export] private Node2D _interactiveItemsParent;

    [Export] private float _scale = 0.1f;
    [Export] private float _gridItemSpawnRate = 0.025f;
    [Export] private float _gridItemBiomeSpawnRate = 0.75f;
    [Export(PropertyHint.Range, "0,1")] private float _interactiveItemSpawnRate = 0.1f;
    [Export] private int _chunkSize = 32;

    private sealed class CommonItem
    {
        public Vector2I FootPrint;
        public DecorLayer.Frame[] Frames;
        public float Speed;
    }

    private sealed class InteractiveItem
    {
        public Vector2I FootPrint;
        public PackedScene Scene;
    }

    private List<CommonItem> _basicCommonItems;
    private Dictionary<BiomeType, List<CommonItem>> _biomeCommonItems;
    private Dictionary<BiomeType, List<InteractiveItem>> _biomeInteractiveItems;

    private GridWorld _world;
    private bool[,] _occupied;
    private int _gridSize, _gridItemSize;

    public async Task GenerateAsync(GridWorld world)
    {
        _world = world;
        LoadCatalog();

        RandomNumberGenerator rng = world.Rng;
        float xOffset = world.RangeInt(-1000, 1000);
        float yOffset = world.RangeInt(-1000, 1000);

        _gridSize = world.GridSize;
        _gridItemSize = _gridSize * Sub;
        _occupied = new bool[_gridItemSize, _gridItemSize];

        var noise = new FastNoiseLite { NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin, Frequency = 1f, FractalType = FastNoiseLite.FractalTypeEnum.None };
        var noiseMap = new float[_gridItemSize, _gridItemSize];
        for (int y = 0; y < _gridItemSize; y++)
        {
            for (int x = 0; x < _gridItemSize; x++)
                noiseMap[x, y] = GridWorld.Perlin01(noise, xOffset + x * _scale, yOffset + y * _scale);
            if (y % 60 == 0) await NextFrame();
        }

        for (int cx = 0; cx < _gridSize; cx += _chunkSize)
        for (int cy = 0; cy < _gridSize; cy += _chunkSize)
        {
            GenerateChunk(rng, noiseMap, cx, cy);
            await NextFrame();
        }
        _decor.QueueRedraw();
    }

    private async Task NextFrame()
    {
        if (!_world.SkipGeneration) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    private void GenerateChunk(RandomNumberGenerator rng, float[,] noiseMap, int chunkX, int chunkY)
    {
        for (int wx = chunkX; wx < Mathf.Min(chunkX + _chunkSize, _gridSize); wx++)
        for (int wy = chunkY; wy < Mathf.Min(chunkY + _chunkSize, _gridSize); wy++)
        {
            TileData tile = _world.GetTileAt(new Vector2I(wx, wy));
            if (tile.Type == TileType.WATER) continue;

            // Une case voisine différente (type ou biome) : on ne pose rien du côté de cette bordure.
            bool diffLeft = wx == 0 || IsDifferent(tile, wx - 1, wy);
            bool diffRight = wx == _gridSize - 1 || IsDifferent(tile, wx + 1, wy);
            bool diffTop = wy == 0 || IsDifferent(tile, wx, wy - 1);               // nord = y - 1
            bool diffBottom = wy == _gridSize - 1 || IsDifferent(tile, wx, wy + 1); // sud = y + 1

            for (int subY = 0; subY < Sub; subY++)
            for (int subX = 0; subX < Sub; subX++)
            {
                if (diffLeft && subX <= 1) continue;
                if (diffRight && subX >= 1) continue;
                if (diffTop && subY <= 1) continue;
                if (diffBottom && subY >= 1) continue;

                int ix = wx * Sub + subX, iy = wy * Sub + subY;
                if (rng.Randf() < 1 - _gridItemSpawnRate) continue;

                // --- Objets interactifs ---
                if (rng.Randf() < _interactiveItemSpawnRate)
                {
                    InteractiveItem interactive = PickInteractive(tile.Biome);
                    if (interactive != null && CanPlace(ix, iy, interactive.FootPrint) && IsValidBiomeArea(ix, iy, interactive.FootPrint, tile))
                    {
                        var node = interactive.Scene.Instantiate<Node2D>();
                        _interactiveItemsParent.AddChild(node);
                        node.GlobalPosition = SubCellCenter(ix, iy);
                        Occupy(ix, iy, interactive.FootPrint);
                        continue;
                    }
                }

                // --- Décor ---
                if (_occupied[ix, iy]) continue;

                CommonItem item = null;
                if (rng.Randf() < _gridItemBiomeSpawnRate)
                {
                    if (_biomeCommonItems.TryGetValue(tile.Biome, out List<CommonItem> biomeItems) && biomeItems.Count > 0)
                        item = biomeItems[NoiseToIndex(noiseMap[ix, iy], biomeItems.Count)];
                }
                else if (_basicCommonItems.Count > 0)
                {
                    item = _basicCommonItems[NoiseToIndex(noiseMap[ix, iy], _basicCommonItems.Count)];
                }

                if (item != null && item.Frames.Length > 0 && CanPlace(ix, iy, item.FootPrint) && IsValidBiomeArea(ix, iy, item.FootPrint, tile))
                {
                    _decor.Add(SubCellCenter(ix, iy), item.Frames, item.Speed);
                    Occupy(ix, iy, item.FootPrint);
                }
            }
        }
    }

    /// <summary>Centre d'une sous-case, en position globale (une case = 16 px, une sous-case = 16/3 px).</summary>
    private Vector2 SubCellCenter(int ix, int iy) =>
        _world.GridToWorld(Vector2I.Zero) - new Vector2(8, 8) + new Vector2(ix + 0.5f, iy + 0.5f) * (PlayerMovements.PixelsPerUnit / Sub);

    // L'empreinte s'étend vers l'est et le NORD (y décroissant), comme dans Unity où y montait.
    private IEnumerable<Vector2I> FootPrintCells(int x, int y, Vector2I footPrint)
    {
        for (int oy = 0; oy < footPrint.Y; oy++)
        for (int ox = 0; ox < footPrint.X; ox++)
            yield return new Vector2I(x + ox, y - oy);
    }

    private bool CanPlace(int x, int y, Vector2I footPrint)
    {
        foreach (Vector2I c in FootPrintCells(x, y, footPrint))
            if (c.X < 0 || c.X >= _gridItemSize || c.Y < 0 || c.Y >= _gridItemSize || _occupied[c.X, c.Y]) return false;
        return true;
    }

    private void Occupy(int x, int y, Vector2I footPrint)
    {
        foreach (Vector2I c in FootPrintCells(x, y, footPrint))
            if (c.X >= 0 && c.X < _gridItemSize && c.Y >= 0 && c.Y < _gridItemSize) _occupied[c.X, c.Y] = true;
    }

    private bool IsValidBiomeArea(int x, int y, Vector2I footPrint, TileData tile)
    {
        foreach (Vector2I c in FootPrintCells(x, y, footPrint))
        {
            int wx = c.X / Sub, wy = c.Y / Sub;
            if (c.X < 0 || c.Y < 0 || wx >= _gridSize || wy >= _gridSize || IsDifferent(tile, wx, wy)) return false;
        }
        return true;
    }

    private bool IsDifferent(TileData current, int x, int y)
    {
        TileData n = _world.GetTileAt(new Vector2I(x, y));
        return n.Type != current.Type || n.Biome != current.Biome;
    }

    private InteractiveItem PickInteractive(BiomeType biome) =>
        _biomeInteractiveItems.TryGetValue(biome, out List<InteractiveItem> items) && items.Count > 0
            ? items[_world.RangeInt(0, items.Count)] : null;

    private static int NoiseToIndex(float noise, int count) => Mathf.Clamp(Mathf.FloorToInt(noise * count), 0, count - 1);

    // ------------------------------------------------------------------ Catalogue (world_items.json)
    private void LoadCatalog()
    {
        using JsonDocument doc = JsonDocument.Parse(FileAccess.GetFileAsString(CatalogPath));
        JsonElement root = doc.RootElement;

        _basicCommonItems = ReadItems(root.GetProperty("basic"));
        _biomeCommonItems = new Dictionary<BiomeType, List<CommonItem>>();
        foreach (JsonProperty b in root.GetProperty("biomes").EnumerateObject())
            _biomeCommonItems[System.Enum.Parse<BiomeType>(b.Name)] = ReadItems(b.Value);

        _biomeInteractiveItems = new Dictionary<BiomeType, List<InteractiveItem>>();
        foreach (JsonProperty b in root.GetProperty("interactive").EnumerateObject())
        {
            var list = new List<InteractiveItem>();
            foreach (JsonElement e in b.Value.EnumerateArray())
                list.Add(new InteractiveItem { FootPrint = ReadVector(e.GetProperty("footprint")), Scene = GD.Load<PackedScene>(e.GetProperty("scene").GetString()) });
            _biomeInteractiveItems[System.Enum.Parse<BiomeType>(b.Name)] = list;
        }
    }

    private static List<CommonItem> ReadItems(JsonElement array)
    {
        var list = new List<CommonItem>();
        foreach (JsonElement e in array.EnumerateArray())
        {
            var frames = new List<DecorLayer.Frame>();
            foreach (JsonElement f in e.GetProperty("frames").EnumerateArray())
            {
                JsonElement r = f.GetProperty("region"), o = f.GetProperty("offset");
                float scale = f.GetProperty("scale").GetSingle();
                frames.Add(new DecorLayer.Frame(
                    GD.Load<Texture2D>(f.GetProperty("texture").GetString()),
                    new Rect2(r[0].GetSingle(), r[1].GetSingle(), r[2].GetSingle(), r[3].GetSingle()),
                    new Vector2(o[0].GetSingle(), o[1].GetSingle()) * scale,
                    scale));
            }
            list.Add(new CommonItem { FootPrint = ReadVector(e.GetProperty("footprint")), Frames = frames.ToArray(), Speed = e.GetProperty("speed").GetSingle() });
        }
        return list;
    }

    private static Vector2I ReadVector(JsonElement a) => new(a[0].GetInt32(), a[1].GetInt32());
}
