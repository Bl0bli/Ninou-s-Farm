using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Godot;

/// <summary>
/// Monde procédural (port de GridWorld.cs) : bruit de Perlin multi-octaves + îles par biome (falloff),
/// falaises lissées, lacs, rivières, petites flaques supprimées, puis peinture des calques et des pentes.
///
/// Correspondances :
///  - coroutines (yield return null) → méthode async qui attend la frame suivante (NextFrame) ;
///  - Mathf.PerlinNoise → FastNoiseLite (Perlin) ramené de [-1, 1] à [0, 1] ;
///  - UnityEngine.Random (InitState) → RandomNumberGenerator avec la même graine ;
///  - Tilemap.SetTiles + RuleTile → RuleTile.Paint sur des TileMapLayer.
///
/// ⚠️ La grille est en coordonnées Godot (Y vers le bas) : « au sud » = y + 1. Seules les pentes en dépendent
/// (la face d'une falaise est au sud) ; le reste de l'algorithme est symétrique.
/// </summary>
public partial class GridWorld : Node2D, ITileGrid
{
    public static GridWorld Instance { get; private set; }

    [Export] private WorldGenConfig _worldGenConfig;

    [ExportGroup("Tilemaps (Layers)")]
    [Export] private TileMapLayer _waterLayer;
    [Export] private TileMapLayer _groundLayer;
    [Export] private TileMapLayer _floorLayer;
    [Export] private TileMapLayer _collisionLayer;

    [ExportGroup("Procedural Generation Params")]
    [Export] private WorldItemsGeneration _itemsGeneration;
    [Export] private float _margeX = 25f;
    [Export] private float _margeY = 25f;
    /// <summary>Taille des blocs peints par frame (chunkSize dans Unity).</summary>
    [Export] private int _chunkSize = 32;
    /// <summary>Largeur de l'océan peint autour de la grille, en cases (ajout du port, voir PaintTilesAsync).</summary>
    [Export] private int _oceanMargin = 30;

    private const string SkipAction = "ui_accept"; // Espace / Entrée : finir la génération sans attendre (comme Unity)

    private TileData[,] _grid;
    private TileGridData _gridData;
    private readonly List<Biome> _biomes = new();
    private Biome[,] _biomeMap;
    private int _rows, _cols;
    private RandomNumberGenerator _rng;
    private string[,] _floorNames; // tuile du calque Floor (falaise ou pente) : nécessaire aux règles des voisins

    public bool SkipGeneration { get; private set; }
    public int GridSize => _worldGenConfig.Size;
    public RandomNumberGenerator Rng => _rng;

    public event Action<Vector2I> OnTileChanged;
    /// <summary>Monde prêt : position du point d'apparition du joueur (OnWorldInit dans Unity).</summary>
    public event Action<Vector2> OnWorldInit;
    /// <summary>Progression de la génération (0 à 1) et étape en cours, pour l'écran de chargement.</summary>
    public event Action<float, string> OnProgress;

    // ------------------------------------------------------------------ ITileGrid
    public TileData GetTileAt(Vector2I p) => _gridData != null ? _gridData.GetTileAt(p) : new TileData(p, BiomeType.WATER, TileType.WATER);
    public Vector2I WorldToGrid(Vector2 worldPosition) => _groundLayer.LocalToMap(_groundLayer.ToLocal(worldPosition));
    public Vector2 GridToWorld(Vector2I gridPosition) => _groundLayer.ToGlobal(_groundLayer.MapToLocal(gridPosition));
    public bool IsOccupied(Vector2I p) => _gridData != null && _gridData.IsOccupied(p);
    public bool TryOccupy(Vector2I p, CropInstance crop) => _gridData != null && _gridData.TryOccupy(p, crop);
    public bool TryGetCrop(Vector2I p, out CropInstance crop)
    {
        crop = null;
        return _gridData != null && _gridData.TryGetCrop(p, out crop);
    }
    public void Release(Vector2I p, CropInstance crop) => _gridData?.Release(p, crop);
    public bool CanBeFarmed(Vector2I p) => _gridData != null && _gridData.CanBeFarmed(p);
    public bool SetTileState(Vector2I p, TileState state) => _gridData != null && _gridData.SetTileState(p, state);
    public bool SetTileWatered(Vector2I p, bool isWatered) => _gridData != null && _gridData.SetTileWatered(p, isWatered);

    // ------------------------------------------------------------------ Cycle de vie
    public override void _EnterTree()
    {
        if (Instance != null && Instance != this)
        {
            QueueFree();
            return;
        }
        Instance = this;
        TileGridLocator.Register(this);
    }

    public override void _ExitTree()
    {
        TileGridLocator.Unregister(this);
        if (Instance == this) Instance = null;
    }

    public override void _Ready() => _ = GenerateWorldAsync();

    public override void _UnhandledInput(InputEvent e)
    {
        if (e.IsActionPressed(SkipAction) && _gridData == null) SkipGeneration = true;
    }

    private async Task NextFrame()
    {
        if (!SkipGeneration) await ToSignal(GetTree(), SceneTree.SignalName.ProcessFrame);
    }

    // ------------------------------------------------------------------ Génération
    private async Task GenerateWorldAsync()
    {
        InitRandom();
        int size = _worldGenConfig.Size;

        float xOffset = _rng.RandiRange(-1000, 999);
        float yOffset = _rng.RandiRange(-1000, 999);
        var grid = new TileData[size, size];
        _grid = grid;
        SkipGeneration = false;

        SetupBiomes();
        ComputeBiomeMap();
        Report(0.05f, "Biomes");
        await NextFrame();

        FastNoiseLite noise = CreateNoise();
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            grid[x, y] = ComputeTile(new Vector2I(x, y), Fbm(noise, x, y, xOffset, yOffset));

        SmoothCliffs(_worldGenConfig.CliffSmoothingIterations);
        Report(0.15f, "Relief");
        await NextFrame();
        GenerateLakes();
        Report(0.2f, "Lacs");
        await NextFrame();
        GenerateRivers();
        Report(0.25f, "Rivières");
        await NextFrame();
        RemoveSmallWaterBodies();
        await NextFrame();

        // La grille logique n'est exposée qu'une fois le relief terminé (lu par le décor et le gameplay).
        _gridData = new TileGridData(grid);
        _gridData.OnTileChanged += cell => OnTileChanged?.Invoke(cell);

        PlaceSlopes();
        await PaintTilesAsync();
        Report(0.85f, "Décor");
        if (_itemsGeneration != null) await _itemsGeneration.GenerateAsync(this);

        Report(1f, "Prêt");
        OnWorldInit?.Invoke(GetSpawnableTile());
    }

    private void Report(float progress, string step) => OnProgress?.Invoke(progress, step);

    private void InitRandom()
    {
        // TickCount : millisecondes depuis le démarrage du système (System.Environment.TickCount dans Unity aussi).
        if (_worldGenConfig.UseRandomSeed) _worldGenConfig.Seed = System.Environment.TickCount;
        _rng = new RandomNumberGenerator { Seed = (ulong)(uint)_worldGenConfig.Seed };
    }

    /// <summary>Random.Range(int min, int max) d'Unity : max exclu, et renvoie min si l'intervalle est vide.</summary>
    public int RangeInt(int min, int maxExclusive) => maxExclusive <= min ? min : _rng.RandiRange(min, maxExclusive - 1);

    private static FastNoiseLite CreateNoise() => new()
    {
        NoiseType = FastNoiseLite.NoiseTypeEnum.Perlin,
        Frequency = 1f,
        FractalType = FastNoiseLite.FractalTypeEnum.None,
    };

    /// <summary>Perlin ramené dans [0, 1], comme Mathf.PerlinNoise.</summary>
    public static float Perlin01(FastNoiseLite noise, float x, float y) =>
        Mathf.Clamp((noise.GetNoise2D(x, y) + 1f) * 0.5f, 0f, 1f);

    /// <summary>
    /// Somme d'octaves (fBm). Repris à l'identique d'Unity, y compris la fréquence appliquée deux fois
    /// (dans sx et dans l'appel au bruit) : c'est elle qui donne son relief au monde existant.
    /// </summary>
    private float Fbm(FastNoiseLite noise, float x, float y, float xOffset, float yOffset)
    {
        float val = 0f, amplitude = 1f, freq = 1f, normalization = 0f;
        for (int i = 0; i < _worldGenConfig.Octaves; i++)
        {
            float sx = (xOffset + x * _worldGenConfig.Scale) * freq;
            float sy = (yOffset + y * _worldGenConfig.Scale) * freq;
            val += amplitude * Perlin01(noise, sx * freq, sy * freq);
            normalization += amplitude;
            amplitude *= _worldGenConfig.Persistence;
            freq *= _worldGenConfig.Lacunarity;
        }
        return val / normalization;
    }

    private void SetupBiomes()
    {
        _biomes.Clear();
        var validBiomes = new List<BiomeType>();
        foreach (BiomeType b in Enum.GetValues<BiomeType>())
            if (b != BiomeType.WATER) validBiomes.Add(b);

        Shuffle(validBiomes);
        int total = validBiomes.Count;
        _cols = Mathf.CeilToInt(Mathf.Sqrt(total));
        _rows = Mathf.CeilToInt((float)total / _cols);

        float sectorWidth = (float)_worldGenConfig.Size / _cols;
        float sectorHeight = (float)_worldGenConfig.Size / _rows;

        int index = 0;
        for (int r = 0; r < _rows; r++)
        for (int c = 0; c < _cols; c++)
        {
            if (index >= total) break;
            float cx = _rng.RandfRange(c * sectorWidth + _margeX, (c + 1) * sectorWidth - _margeX);
            float cy = _rng.RandfRange(r * sectorHeight + _margeY, (r + 1) * sectorHeight - _margeY);
            _biomes.Add(new Biome(new Vector2(cx, cy), validBiomes[index++]));
        }
    }

    /// <summary>Mélange repris d'Unity tel quel (Random.Range(0, i) exclut i : ce n'est pas un Fisher-Yates exact).</summary>
    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int index = RangeInt(0, i);
            (list[i], list[index]) = (list[index], list[i]);
        }
    }

    private void ComputeBiomeMap()
    {
        int size = _worldGenConfig.Size;
        _biomeMap = new Biome[size, size];
        for (int x = 0; x < size; x++)
        for (int y = 0; y < size; y++)
        {
            var pos = new Vector2(x, y);
            Biome closest = _biomes[0];
            float best = float.MaxValue;
            foreach (Biome b in _biomes)
            {
                float d = (pos - b.Position).LengthSquared();
                if (d < best) { best = d; closest = b; }
            }
            _biomeMap[x, y] = closest;
        }
    }

    private Biome GetClosestBiome(Vector2 position)
    {
        int x = Mathf.Clamp(Mathf.RoundToInt(position.X), 0, _worldGenConfig.Size - 1);
        int y = Mathf.Clamp(Mathf.RoundToInt(position.Y), 0, _worldGenConfig.Size - 1);
        return _biomeMap[x, y];
    }

    private TileData ComputeTile(Vector2I position, float baseNoise)
    {
        Biome local = GetClosestBiome(position);
        float distToCenter = ((Vector2)position).DistanceTo(local.Position);
        float maxIslandRadius = (_worldGenConfig.Size / (float)Mathf.Max(_rows, _cols)) * _worldGenConfig.IslandRadiusFactor;
        float islandFalloff = Mathf.Clamp(distToCenter / maxIslandRadius, 0f, 1f);

        float finalHeight = baseNoise * (1 - islandFalloff);

        if (finalHeight < _worldGenConfig.SeaLevel) return new TileData(position, BiomeType.WATER, TileType.WATER, 0, finalHeight);
        if (finalHeight > _worldGenConfig.CliffLevel) return new TileData(position, local.Type, TileType.FLOOR, 0, finalHeight);
        return new TileData(position, local.Type, TileType.GROUND, 0, finalHeight);
    }

    private void SmoothCliffs(int iterations)
    {
        int size = _worldGenConfig.Size;
        var next = new TileType[size, size];
        for (int i = 0; i < iterations; i++)
        {
            for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
            {
                next[x, y] = _grid[x, y].Type;
                if (next[x, y] == TileType.WATER) continue;

                int n = GetNeighbours(x, y, TileType.FLOOR);
                if (n > _worldGenConfig.FloorNeighboursRequired) next[x, y] = TileType.FLOOR;
                else if (n < _worldGenConfig.FloorNeighboursRequired) next[x, y] = TileType.GROUND;
            }
            for (int x = 0; x < size; x++)
            for (int y = 0; y < size; y++)
                _grid[x, y].Type = next[x, y];
        }
    }

    private int GetNeighbours(int x, int y, TileType type)
    {
        int count = 0, size = _worldGenConfig.Size;
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = x + dx, ny = y + dy;
            if (nx >= 0 && nx < size && ny >= 0 && ny < size && _grid[nx, ny].Type == type) count++;
        }
        return count;
    }

    // ------------------------------------------------------------------ Lacs
    private void GenerateLakes()
    {
        foreach (Biome biome in _biomes)
        {
            int created = 0, attempts = 0;
            while (created < _worldGenConfig.MaxLakesPerBiome && attempts < 100)
            {
                attempts++;
                int sx = RangeInt(1, _worldGenConfig.Size - 1);
                int sy = RangeInt(1, _worldGenConfig.Size - 1);
                if (_grid[sx, sy].Type == TileType.GROUND && _grid[sx, sy].Biome == biome.Type)
                {
                    CarveLake(sx, sy);
                    created++;
                }
            }
        }
    }

    private void CarveLake(int startX, int startY)
    {
        int size = _worldGenConfig.Size;
        int targetSize = RangeInt(_worldGenConfig.MaxLakeSize / 2, _worldGenConfig.MaxLakeSize);
        var carved = new List<Vector2I>();
        var frontier = new List<Vector2I> { new(startX, startY) };

        while (carved.Count < targetSize && frontier.Count > 0) // remplissage par diffusion aléatoire
        {
            int i = RangeInt(0, frontier.Count);
            Vector2I p = frontier[i];
            frontier.RemoveAt(i);

            if (p.X < 0 || p.X >= size || p.Y < 0 || p.Y >= size) continue;
            if (_grid[p.X, p.Y].Type != TileType.GROUND) continue;

            _grid[p.X, p.Y].Type = TileType.WATER; // Height et Biome restent intacts
            carved.Add(p);
            frontier.Add(p + Vector2I.Right);
            frontier.Add(p + Vector2I.Left);
            frontier.Add(p + Vector2I.Down);
            frontier.Add(p + Vector2I.Up);
        }

        // Un lac trop petit est disgracieux : on annule plutôt que de laisser une flaque.
        if (carved.Count < _worldGenConfig.MinLakeSize)
            foreach (Vector2I p in carved) _grid[p.X, p.Y].Type = TileType.GROUND;
    }

    /// <summary>
    /// Supprime les petites poches d'eau parasites : on garde la mer (touche le bord) et les vrais lacs
    /// (>= MinLakeSize) ; le reste redevient de la terre (biome local, type selon la hauteur).
    /// </summary>
    private void RemoveSmallWaterBodies()
    {
        int size = _worldGenConfig.Size;
        var visited = new bool[size, size];
        Vector2I[] dirs = { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right };

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            if (_grid[x, y].Type != TileType.WATER || visited[x, y]) continue;

            var component = new List<Vector2I>();
            var queue = new Queue<Vector2I>();
            queue.Enqueue(new Vector2I(x, y));
            visited[x, y] = true;
            bool touchesBorder = false;

            while (queue.Count > 0)
            {
                Vector2I cur = queue.Dequeue();
                component.Add(cur);
                if (cur.X == 0 || cur.X == size - 1 || cur.Y == 0 || cur.Y == size - 1) touchesBorder = true;

                foreach (Vector2I d in dirs)
                {
                    int nx = cur.X + d.X, ny = cur.Y + d.Y;
                    if (nx < 0 || nx >= size || ny < 0 || ny >= size) continue;
                    if (visited[nx, ny] || _grid[nx, ny].Type != TileType.WATER) continue;
                    visited[nx, ny] = true;
                    queue.Enqueue(new Vector2I(nx, ny));
                }
            }

            if (touchesBorder || component.Count >= _worldGenConfig.MinLakeSize) continue;

            foreach (Vector2I c in component)
            {
                _grid[c.X, c.Y].Biome = GetClosestBiome(c).Type;
                _grid[c.X, c.Y].Type = _grid[c.X, c.Y].Height > _worldGenConfig.CliffLevel ? TileType.FLOOR : TileType.GROUND;
            }
        }
    }

    // ------------------------------------------------------------------ Rivières
    private void GenerateRivers()
    {
        int size = _worldGenConfig.Size;
        foreach (Biome biome in _biomes)
        {
            // Sources : falaises en bordure de plateau (au moins un voisin GROUND).
            var sources = new List<Vector2I>();
            for (int y = 1; y < size - 1; y++)
            for (int x = 1; x < size - 1; x++)
                if (_grid[x, y].Type == TileType.FLOOR && _grid[x, y].Biome == biome.Type && GetNeighbours(x, y, TileType.GROUND) > 0)
                    sources.Add(new Vector2I(x, y));

            if (sources.Count == 0) continue;

            int numRivers = RangeInt(0, _worldGenConfig.MaxRiversPerBiome + 1);
            int placed = 0, attempts = 0, maxAttempts = sources.Count * 2;
            // Une rivière n'est retenue que si son tracé atteint de l'eau ou le bord de la carte.
            while (placed < numRivers && attempts < maxAttempts)
            {
                attempts++;
                Vector2I source = sources[RangeInt(0, sources.Count)];
                var path = new List<Vector2I>();
                if (TryTraceRiver(source.X, source.Y, path))
                {
                    CarveRiverPath(path);
                    placed++;
                }
            }
            GD.Print($"[Rivers] Biome {biome.Type} : {placed}/{numRivers} rivière(s) placée(s) en {attempts} essais ({sources.Count} sources).");
        }
    }

    /// <summary>Trajet d'une goutte par descente de gradient + inertie, sans rien creuser.</summary>
    private bool TryTraceRiver(int startX, int startY, List<Vector2I> path)
    {
        int size = _worldGenConfig.Size;
        var pos = new Vector2(startX, startY);
        Vector2 dir = Vector2.Zero;
        float inertia = _worldGenConfig.RiverInertia;

        for (int i = 0; i < _worldGenConfig.MaxRiverLength; i++)
        {
            Vector2 gradient = HeightGradient(Mathf.RoundToInt(pos.X), Mathf.RoundToInt(pos.Y));
            dir = dir * inertia - gradient * (1f - inertia);

            if (dir.LengthSquared() < 1e-6f) // terrain plat, aucun élan : petite poussée aléatoire
            {
                float a = _rng.Randf() * Mathf.Pi * 2f;
                dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            }
            dir = dir.Normalized();
            pos += dir;

            int nx = Mathf.RoundToInt(pos.X), ny = Mathf.RoundToInt(pos.Y);
            if (nx < 0 || nx >= size || ny < 0 || ny >= size) return true; // se jette dans l'océan
            if (_grid[nx, ny].Type == TileType.WATER) return true;          // rejoint mer, lac ou rivière
            path.Add(new Vector2I(nx, ny));
        }
        return false; // longueur max atteinte en pleine terre : la rivière ne relie rien
    }

    private void CarveRiverPath(List<Vector2I> path)
    {
        int radius = _worldGenConfig.MaxRiverThickness;
        for (int i = 0; i < path.Count; i++)
        {
            Vector2I p = path[i];
            if (i > 0)
            {
                Vector2I prev = path[i - 1];
                if (p.X != prev.X && p.Y != prev.Y) CarveCell(p.X, prev.Y); // pont : l'eau reste connectée sans diagonale
            }
            for (int tx = -radius; tx <= radius; tx++)
            for (int ty = -radius; ty <= radius; ty++)
                if (tx * tx + ty * ty <= radius * radius) CarveCell(p.X + tx, p.Y + ty);
        }
    }

    private Vector2 HeightGradient(int x, int y) => new(
        (HeightAt(x + 1, y) - HeightAt(x - 1, y)) * 0.5f,
        (HeightAt(x, y + 1) - HeightAt(x, y - 1)) * 0.5f);

    private float HeightAt(int x, int y)
    {
        int max = _worldGenConfig.Size - 1;
        return _grid[Mathf.Clamp(x, 0, max), Mathf.Clamp(y, 0, max)].Height;
    }

    private void CarveCell(int x, int y)
    {
        int size = _worldGenConfig.Size;
        if (x < 0 || x >= size || y < 0 || y >= size) return;
        if (_grid[x, y].Type == TileType.FLOOR) return; // on ne creuse pas les falaises
        _grid[x, y].Type = TileType.WATER;
    }

    // ------------------------------------------------------------------ Pentes
    /// <summary>
    /// Pour chaque plateau (zone FLOOR connexe), cherche les bords sud droits (4 FLOOR alignés au-dessus
    /// de 4 GROUND) et y pose quelques pentes 2x2. Unity faisait ce calcul après la peinture et posait les
    /// tuiles directement ; ici on note la tuile de chaque case dans _floorNames, peinte ensuite en une fois.
    /// </summary>
    private void PlaceSlopes()
    {
        int size = _worldGenConfig.Size;
        _floorNames = new string[size, size];
        for (int x = 0; x < size; x++)
        for (int y = 0; y < size; y++)
            if (_grid[x, y].Type == TileType.FLOOR) _floorNames[x, y] = FloorTileName(_grid[x, y].Biome);

        var visited = new bool[size, size];
        for (int x = 0; x < size; x++)
        for (int y = 0; y < size; y++)
        {
            if (_grid[x, y].Type != TileType.FLOOR || visited[x, y]) continue;

            var queue = new Queue<Vector2I>();
            var potential = new List<Vector2I>();
            queue.Enqueue(new Vector2I(x, y));
            visited[x, y] = true;

            while (queue.Count > 0)
            {
                Vector2I c = queue.Dequeue();
                if (c.X - 1 >= 0 && c.X + 2 < size && c.Y + 1 < size)
                {
                    bool topRowIsFloor = true, bottomRowIsGround = true;
                    for (int dx = -1; dx <= 2; dx++)
                    {
                        topRowIsFloor &= _grid[c.X + dx, c.Y].Type == TileType.FLOOR;
                        bottomRowIsGround &= _grid[c.X + dx, c.Y + 1].Type == TileType.GROUND; // sud = y + 1
                    }
                    if (topRowIsFloor && bottomRowIsGround) potential.Add(c);
                }
                foreach (Vector2I d in new[] { Vector2I.Up, Vector2I.Down, Vector2I.Left, Vector2I.Right })
                {
                    Vector2I n = c + d;
                    if (n.X < 0 || n.X >= size || n.Y < 0 || n.Y >= size) continue;
                    if (_grid[n.X, n.Y].Type != TileType.FLOOR || visited[n.X, n.Y]) continue;
                    visited[n.X, n.Y] = true;
                    queue.Enqueue(n);
                }
            }

            if (potential.Count == 0) continue;
            int toPlace = Mathf.Clamp(potential.Count / _worldGenConfig.HorizontalDistBetweenSlopes, 1, _worldGenConfig.MaxSlopesPerHill);
            string prefix = $"slope_{_grid[x, y].Biome.ToString().ToLowerInvariant()}_";

            for (int i = 0; i < toPlace && potential.Count > 0; i++)
            {
                Vector2I a = potential[RangeInt(0, potential.Count)];
                // [haut-gauche, haut-droite, bas-gauche, bas-droite] ; « bas » = au sud = y + 1
                _floorNames[a.X, a.Y] = prefix + 0;
                _floorNames[a.X + 1, a.Y] = prefix + 1;
                _floorNames[a.X, a.Y + 1] = prefix + 2;
                _floorNames[a.X + 1, a.Y + 1] = prefix + 3;
                potential.RemoveAll(p => p.X >= a.X - 2 && p.X <= a.X + 2 && p.Y == a.Y);
            }
        }
    }

    // Les collines (HILLS) gardent les noms « ground » / « floor » de la scène de test ; string.Intern : une seule
    // instance par nom, la comparaison des voisins reste rapide.
    private static string GroundTileName(BiomeType biome) =>
        string.Intern(biome == BiomeType.HILLS ? "ground" : $"{biome.ToString().ToLowerInvariant()}_ground");
    private static string FloorTileName(BiomeType biome) =>
        string.Intern(biome == BiomeType.HILLS ? "floor" : $"{biome.ToString().ToLowerInvariant()}_floor");

    // ------------------------------------------------------------------ Peinture
    /// <summary>
    /// Peint les calques par blocs, un bloc par frame (CollapseTilesRoutine). L'eau partout, la collision
    /// sur l'eau, le sol sur GROUND et FLOOR (avec la RuleTile du biome), et le calque Floor (falaises et pentes).
    /// </summary>
    private async Task PaintTilesAsync()
    {
        int size = _worldGenConfig.Size;
        RuleTile water = RuleTile.Get("water");
        RuleTile.TileRef blocker = RuleTile.Blocker;
        var tileCache = new Dictionary<string, RuleTile>();
        RuleTile Tile(string name) => tileCache.TryGetValue(name, out RuleTile t) ? t : tileCache[name] = RuleTile.Get(name);

        // Nom de la tuile de sol de chaque case, calculé une fois (les règles interrogent ~8 voisins par case).
        var groundNames = new string[size, size];
        for (int x = 0; x < size; x++)
        for (int y = 0; y < size; y++)
            if (_grid[x, y].Type != TileType.WATER) groundNames[x, y] = GroundTileName(_grid[x, y].Biome);

        string GroundName(Vector2I c) => c.X < 0 || c.X >= size || c.Y < 0 || c.Y >= size ? null : groundNames[c.X, c.Y];
        string FloorName(Vector2I c) => c.X < 0 || c.X >= size || c.Y < 0 || c.Y >= size ? null : _floorNames[c.X, c.Y];

        // Les collisions sont coupées pendant la peinture (comme les TilemapCollider2D dans Unity) :
        // reconstruire les formes à chaque bloc coûterait cher.
        _groundLayer.CollisionEnabled = false;
        _floorLayer.CollisionEnabled = false;
        _collisionLayer.CollisionEnabled = false;

        int chunks = Mathf.CeilToInt(size / (float)_chunkSize), done = 0;
        for (int cx = 0; cx < size; cx += _chunkSize)
        for (int cy = 0; cy < size; cy += _chunkSize)
        {
            for (int x = cx; x < Mathf.Min(cx + _chunkSize, size); x++)
            for (int y = cy; y < Mathf.Min(cy + _chunkSize, size); y++)
            {
                var cell = new Vector2I(x, y);
                water.Paint(_waterLayer, cell, _ => true);

                string ground = GroundName(cell);
                if (ground == null)
                {
                    _collisionLayer.SetCell(cell, blocker.Source, blocker.Atlas, blocker.Alternative);
                    continue;
                }
                RuleTile groundTile = Tile(ground);
                groundTile.Paint(_groundLayer, cell, n => GroundName(n) == ground);

                string floor = _floorNames[x, y];
                if (floor == null) continue;
                RuleTile floorTile = Tile(floor);
                floorTile.Paint(_floorLayer, cell, n => floorTile.Matches(FloorName(n)));
            }

            done++;
            Report(0.25f + 0.6f * done / (chunks * chunks), "Tuiles");
            await NextFrame();
        }

        // Ajout du port : un anneau d'océan (bloquant) autour de la grille. Les îles touchent souvent le bord ;
        // sans lui on verrait du vide au-delà et le joueur pourrait sortir de la carte.
        for (int x = -_oceanMargin; x < size + _oceanMargin; x++)
        for (int y = -_oceanMargin; y < size + _oceanMargin; y++)
        {
            if (x >= 0 && x < size && y >= 0 && y < size) continue;
            var cell = new Vector2I(x, y);
            water.Paint(_waterLayer, cell, _ => true);
            _collisionLayer.SetCell(cell, blocker.Source, blocker.Atlas, blocker.Alternative);
        }

        _groundLayer.CollisionEnabled = true;
        _floorLayer.CollisionEnabled = true;
        _collisionLayer.CollisionEnabled = true;
    }

    private Vector2 GetSpawnableTile()
    {
        var valid = new List<Vector2I>();
        int size = _worldGenConfig.Size;
        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
            if (_grid[x, y].Biome == BiomeType.HILLS && _grid[x, y].Type == TileType.GROUND && _floorNames[x, y] == null)
                valid.Add(new Vector2I(x, y));

        if (valid.Count > 0) return GridToWorld(valid[RangeInt(0, valid.Count)]);

        GD.PushWarning("Aucun point de spawn valide trouvé dans le biome Hills. Spawn au centre par défaut.");
        return GridToWorld(new Vector2I(size / 2, size / 2));
    }
}
