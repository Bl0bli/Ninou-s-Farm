using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Tilemaps;
using Random = UnityEngine.Random;

[System.Serializable]
public struct BiomeRuleTileMapping
{
    public BiomeType Biome;
    public TileType Type;
    public RuleTile RuleTile;
}

[Serializable]
public struct Slope
{
    public BiomeType Biome;
    public TileBase[] Tile;
}
public class GridWorld : MonoBehaviour
{
    [SerializeField] private UnityEvent _onWorldInit;
    [SerializeField] private Camera _camera;
    
    [SerializeField] private SO_WorldGenConfig _worldGenConfig;
    
    [Header("Tilemaps (Layers)")] 
    [SerializeField] private Tilemap _waterTilemap;
    [SerializeField] private Tilemap _groundTilemap;
    [SerializeField] private Tilemap _floorTilemap;
    [SerializeField] private Tilemap _collisionTilemap;
    [SerializeField] private List<Slope> _slopes = new List<Slope>();
    [SerializeField] private List<BiomeRuleTileMapping> _tileMappings;

    [Header("Procedural Generation Params")] 
    
    [SerializeField] private int current;

    [SerializeField] private float currentFPS;
    [SerializeField] int _MINtilesPerFrame = 100;
    [SerializeField] int _MAXtilesPerFrame = 500;
    [SerializeField] private float _minFPS = 30f;          // Le seuil critique de lag
    [SerializeField] private float _maxFPS = 60f;
    [SerializeField] WorldItemsGeneration _itemsGeneration;

    [SerializeField] private float _waterPercentage = 0.45f;
    [SerializeField] private float _wallPercentage = 0.1f;
    [SerializeField] private float _margeX;
    [SerializeField] private float _margeY;
    [SerializeField, Range(1f, 10f)] private float _falloffPower = 3f;
    [SerializeField, Range(1f, 5f)] private float _falloffRange = 2.2f;
    
    private TileData[,] _grid;
    private List<Biome> _biomes = new List<Biome>();
    private Biome[,] _biomeMap;
    public bool SkipGeneration { get; private set; }
    private Dictionary<(BiomeType, TileType), RuleTile> _ruleTileCache;
    
    private int _rows, _cols;

    /// <summary>
    /// Récupère les données de la tuile à une position de grille spécifique.
    /// </summary>
    /// <param name="gridPos">La position dans la grille.</param>
    /// <returns>Les données de la tuile à cette position.</returns>
    public TileData GetTileAt(Vector2Int gridPos) => _grid[gridPos.x, gridPos.y];

    /// <summary>
    /// Récupère les données de la tuile correspondant à une position dans le monde.
    /// </summary>
    /// <param name="worldPos">La position dans l'espace mondial.</param>
    /// <returns>Les données de la tuile correspondante.</returns>
    public TileData GetTileAt(Vector3 worldPos)
    {
        //Debug.Log(dir);
        Vector3Int cellPos = _groundTilemap.WorldToCell(worldPos);
        if (cellPos.x >= 0 && cellPos.x < _worldGenConfig.Size && cellPos.y >= 0 && cellPos.y < _worldGenConfig.Size)
            return _grid[cellPos.x, cellPos.y];
        return new TileData(new Vector2Int(cellPos.x, cellPos.y), BiomeType.WATER, TileType.WATER);
    }
    public int GridSize => _worldGenConfig.Size;
    public static GridWorld Instance;
    public event Action<Vector2> OnWorldInit;
    private void Awake()
    {
        if(Instance != null) Destroy(this);
        Instance = this;
    }

    private void Start()
    {
        _camera.transform.position = new Vector3(_worldGenConfig.Size / 2, _worldGenConfig.Size / 2, -10);
        _camera.orthographicSize = _worldGenConfig.Size / 2;
        _ruleTileCache = new Dictionary<(BiomeType, TileType), RuleTile>();
        foreach (BiomeRuleTileMapping mapping in _tileMappings)
        {
            _ruleTileCache[(mapping.Biome, mapping.Type)] = mapping.RuleTile;
        }
        GameManager.Instance.Init();

        StartCoroutine(GenerateWorldRoutine());
    }

    [ContextMenu("Regenerate World")]
    private void Regenerate()
    {
        if (!Application.isPlaying)
        {
            Debug.LogError("Regenerate only in PlayMode.");
            return;
        }
        
        StopAllCoroutines();
        ResetWorld();
        StartCoroutine(GenerateWorldRoutine());   
        
    }

    [ContextMenu("Debug HeightMap")]
    private void DebugHeightMap()
    {
        int n = _worldGenConfig.Size;
        Texture2D tex = new Texture2D(n, n);
        for (int y = 0; y < n; y++)
        {
            for (int x = 0; x < n; x++)
            {
                float height = _grid[x, y].Height;
                tex.SetPixel(x, y, new Color(height, height, height));
            }
        }
        tex.Apply();
        System.IO.File.WriteAllBytes(
            Application.dataPath + "/heightmap_debug.png", tex.EncodeToPNG());
        Debug.Log("Heightmap exportée dans Assets/heightmap_debug.png");
    }

    private float Fbm(float x, float y, float xOffset, float yOffset)
    {
        float val = 0f;
        float amplitude = 1f;
        float freq = 1f;
        float normalization = 0f;
        
        for(int i = 0; i < _worldGenConfig.Octaves; i++)
        {
            
            float sx = (xOffset + x * _worldGenConfig.Scale) * freq;
            float sy = (yOffset + y * _worldGenConfig.Scale) * freq;
            val += amplitude * Mathf.PerlinNoise(sx * freq, sy * freq);
            normalization += amplitude;
            amplitude *= _worldGenConfig.Persistence;
            freq *= _worldGenConfig.Lacunarity;
        }

        return val / normalization;
    }

    private void InitRandom()
    {
        if (_worldGenConfig.UseRandomSeed) _worldGenConfig.Seed = System.Environment.TickCount; //le nombre de millisecondes écoulées depuis le démarrage du système
        
        Random.InitState(_worldGenConfig.Seed); //defini l'état du RNG
    }

    private void ResetWorld()
    {
        _waterTilemap.ClearAllTiles();
        _groundTilemap.ClearAllTiles();
        _floorTilemap.ClearAllTiles();
        _collisionTilemap.ClearAllTiles();
        _biomes.Clear();
        _itemsGeneration.Clear(); 
    }
    
    IEnumerator GenerateWorldRoutine()
    {
        InitRandom();
        
        Dictionary<float, TileType> tilesTypeThresholds = new Dictionary<float, TileType>();
        SetupThresholds(tilesTypeThresholds);
        
        float xoffset = Random.Range(-1000, 1000);
        float yOffset = Random.Range(-1000, 1000);
        _grid = new TileData[_worldGenConfig.Size, _worldGenConfig.Size];
        SkipGeneration = false;

        SetupBiomes();
        ComputeBiomeMap();

        yield return null;
        
        float[,] noiseMap = new float[_worldGenConfig.Size, _worldGenConfig.Size];
        for (int y = 0; y < _worldGenConfig.Size; y++)
        {
            for (int x = 0; x < _worldGenConfig.Size; x++)
            {
                noiseMap[x, y] = Fbm(x, y, xoffset, yOffset);
            }
        }

        for (int y = 0; y < _worldGenConfig.Size; y++)
        {
            for (int x = 0; x < _worldGenConfig.Size; x++)
            {
                // On passe uniquement la position et le bruit brut, la fonction s'occupe du reste
                TileData tileData = ComputeTile(new Vector2Int(x, y), noiseMap[x, y]);
                _grid[x, y] = tileData;
            }
        }

        SmoothCliffs(_worldGenConfig.CliffSmoothingIterations);
        yield return null;
        GenerateLakes();
        yield return null;
        GenerateRivers();
        yield return null;
        RemoveSmallWaterBodies();
        yield return null;
        yield return StartCoroutine(CollapseTilesRoutine());
        yield return StartCoroutine(_itemsGeneration.InitRoutine());
        
        OnWorldInit?.Invoke(GetSpawnableTile());
        _onWorldInit?.Invoke();
    }

    private IEnumerator CollapseTilesRoutine()
    {
        RuleTile waterTile = GetRuleTile(BiomeType.WATER, TileType.WATER);
        
        TilemapCollider2D collisionTilemapCollider = _collisionTilemap.GetComponent<TilemapCollider2D>();
        TilemapCollider2D groundTilemapCollider = _groundTilemap.GetComponent<TilemapCollider2D>();
        
        if (collisionTilemapCollider != null) collisionTilemapCollider.enabled = false;
        if (groundTilemapCollider != null) groundTilemapCollider.enabled = false;

        int chunkSize = 32; 
        
        for (int chunkX = 0; chunkX < _worldGenConfig.Size; chunkX += chunkSize)
        {
            for (int chunkY = 0; chunkY < _worldGenConfig.Size; chunkY += chunkSize)
            {
                if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.isPressed) SkipGeneration = true;
                
                List<Vector3Int> waterPos = new List<Vector3Int>();
                List<TileBase> waterTiles = new List<TileBase>();
                
                List<Vector3Int> collisionPos = new List<Vector3Int>();
                List<TileBase> collisionTiles = new List<TileBase>();

                List<Vector3Int> groundPos = new List<Vector3Int>();
                List<TileBase> groundTiles = new List<TileBase>();

                List<Vector3Int> floorPos = new List<Vector3Int>();
                List<TileBase> floorTiles = new List<TileBase>();

                int endX = Mathf.Min(chunkX + chunkSize, _worldGenConfig.Size);
                int endY = Mathf.Min(chunkY + chunkSize, _worldGenConfig.Size);

                for (int x = chunkX; x < endX; x++)
                {
                    for (int y = chunkY; y < endY; y++)
                    {
                        TileData tileData = _grid[x, y];
                        Vector3Int pos = new Vector3Int(x, y, 0);
                        
                        if (waterTile != null)
                        {
                            waterPos.Add(pos);
                            waterTiles.Add(waterTile);
                        }

                        if (tileData.Type == TileType.WATER && waterTile != null)
                        {
                            collisionPos.Add(pos);
                            collisionTiles.Add(waterTile);
                        }
                        
                        if (tileData.Type == TileType.GROUND || tileData.Type == TileType.FLOOR)
                        {
                            RuleTile groundTile = GetRuleTile(tileData.Biome, TileType.GROUND);
                            if (groundTile != null)
                            {
                                groundPos.Add(pos);
                                groundTiles.Add(groundTile);
                            }
                        }
                        
                        if (tileData.Type == TileType.FLOOR)
                        {
                            RuleTile floorTile = GetRuleTile(tileData.Biome, TileType.FLOOR);
                            if (floorTile != null)
                            {
                                floorPos.Add(pos);
                                floorTiles.Add(floorTile);
                            }
                        }
                    }
                }
                
                if (waterPos.Count > 0) _waterTilemap.SetTiles(waterPos.ToArray(), waterTiles.ToArray());
                if (collisionPos.Count > 0) _collisionTilemap.SetTiles(collisionPos.ToArray(), collisionTiles.ToArray());
                if (groundPos.Count > 0) _groundTilemap.SetTiles(groundPos.ToArray(), groundTiles.ToArray());
                if (floorPos.Count > 0) _floorTilemap.SetTiles(floorPos.ToArray(), floorTiles.ToArray());

                if (!SkipGeneration)
                {
                    yield return null; 
                }
            }
        }

        yield return null;
        
        if (collisionTilemapCollider != null) collisionTilemapCollider.enabled = true;
        if (groundTilemapCollider != null) groundTilemapCollider.enabled = true;
        
        bool[,] visited = new bool[_worldGenConfig.Size, _worldGenConfig.Size];
        for (int x = 0; x < _worldGenConfig.Size; x++)
        {
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.spaceKey.isPressed) SkipGeneration = true;
            for (int y = 0; y < _worldGenConfig.Size; y++)
            {
                TileData tile = _grid[x, y];
                if (tile.Type == TileType.FLOOR && !visited[x, y])
                {
                    Queue<Vector2Int> tilesToCheck = new Queue<Vector2Int>();
                    List<Vector2Int> potentialSlopes = new List<Vector2Int>();
                    tilesToCheck.Enqueue(new Vector2Int(x, y));
                    visited[x, y] = true;

                    while (tilesToCheck.Count > 0)
                    {
                        Vector2Int currentTile = tilesToCheck.Dequeue();
                        if (currentTile.x - 1 >= 0 && currentTile.x + 2 < _worldGenConfig.Size && currentTile.y - 1 >= 0)                        
                        {
                            bool topRowIsFloor = 
                                _grid[currentTile.x - 1, currentTile.y].Type == TileType.FLOOR &&
                                _grid[currentTile.x, currentTile.y].Type == TileType.FLOOR &&
                                _grid[currentTile.x + 1, currentTile.y].Type == TileType.FLOOR &&
                                _grid[currentTile.x + 2, currentTile.y].Type == TileType.FLOOR;  
                            
                            bool bottomRowIsGround = 
                                _grid[currentTile.x - 1, currentTile.y - 1].Type == TileType.GROUND &&
                                _grid[currentTile.x, currentTile.y - 1].Type == TileType.GROUND &&
                                _grid[currentTile.x + 1, currentTile.y - 1].Type == TileType.GROUND &&
                                _grid[currentTile.x + 2, currentTile.y - 1].Type == TileType.GROUND;
                            
                            if (topRowIsFloor && bottomRowIsGround)
                            {
                                potentialSlopes.Add(currentTile);
                            }
                        }
                        Propagate(currentTile, visited, tilesToCheck);
                    }

                    if (potentialSlopes.Count > 0)
                    {
                        int slopesToPlace = Mathf.Clamp(potentialSlopes.Count / _worldGenConfig.HorizontalDistBetweenSlopes, 1, _worldGenConfig.MaxSlopesPerHill);
                        CollapseTilesToSlopes(potentialSlopes, slopesToPlace, tile.Biome);
                    }
                }
            }
            if (!SkipGeneration && x % 5 == 0) yield return null;
        }
    }

    private void CollapseTilesToSlopes(List<Vector2Int> potentialSlopes, int slopesToPlace, BiomeType biome)
    {
        TileBase[] tile = null; 
        foreach (Slope slope in _slopes)
        {
            if (slope.Biome == biome)
            {
                tile = slope.Tile;
                break;
            }
        }
        
        if (tile == null) return; 
        for (int i = 0; i < slopesToPlace; i++)
        {
            if (potentialSlopes.Count == 0) break;

            int randIndex = Random.Range(0, potentialSlopes.Count);
            Vector2Int anchor = potentialSlopes[randIndex];
            
            _floorTilemap.SetTile(new Vector3Int(anchor.x, anchor.y, 0), tile[0]);
            _floorTilemap.SetTile(new Vector3Int(anchor.x + 1, anchor.y, 0), tile[1]);
            _floorTilemap.SetTile(new Vector3Int(anchor.x, anchor.y - 1, 0), tile[2]);
            _floorTilemap.SetTile(new Vector3Int(anchor.x + 1, anchor.y - 1, 0), tile[3]);
            
            potentialSlopes.RemoveAll(p => p.x >= anchor.x - 2 && p.x <= anchor.x + 2 && p.y == anchor.y);
        }
    }

    private void Propagate(Vector2Int currentTile, bool[,] visited, Queue<Vector2Int> tilesToCheck)
    {
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };
        foreach (Vector2Int dir in dirs)
        {
            Vector2Int pos = currentTile + dir;
            if (pos.x >= 0 && pos.x < _worldGenConfig.Size && pos.y >= 0 && pos.y < _worldGenConfig.Size)
            {
                TileData tile = _grid[pos.x, pos.y];
                if (tile.Type == TileType.FLOOR && !visited[pos.x, pos.y])
                {
                    visited[pos.x, pos.y] = true;
                    tilesToCheck.Enqueue(tile.Position);
                }
            }
        }
    }

    private Vector2 GetSpawnableTile()
    {
        List<Vector2Int> validSpawnPoints = new List<Vector2Int>();
        for (int y = 0; y < _worldGenConfig.Size; y++)
        {
            for (int x = 0; x < _worldGenConfig.Size; x++)
            {
                TileData tile = _grid[x, y];
                
                if (tile.Biome == BiomeType.HILLS && tile.Type == TileType.GROUND)
                {
                    validSpawnPoints.Add(new Vector2Int(x, y));
                }
            }
        }
        
        if (validSpawnPoints.Count > 0)
        {
            Vector2Int gridPos = validSpawnPoints[Random.Range(0, validSpawnPoints.Count)];
            Vector3 worldPos = _groundTilemap.GetCellCenterWorld(new Vector3Int(gridPos.x, gridPos.y, 0));
            
            return new Vector2(worldPos.x, worldPos.y);
        }

        Debug.LogWarning("Aucun point de spawn valide trouvé dans le biome Hills. Spawn au centre par défaut.");
        
        Vector3 defaultWorldPos = _groundTilemap.GetCellCenterWorld(new Vector3Int(_worldGenConfig.Size / 2, _worldGenConfig.Size / 2, 0));
        return new Vector2(defaultWorldPos.x, defaultWorldPos.y);
    }
    
    /// <summary>
    /// Convertit une position de grille en position mondiale selon le type de tuile.
    /// </summary>
    /// <param name="gridpos">La position dans la grille.</param>
    /// <param name="type">Le type de tuile pour déterminer la couche.</param>
    /// <returns>La position mondiale au centre de la cellule.</returns>
    public Vector2 GridToWorldPos(Vector2Int gridpos, TileType type)
    {
        switch (type)
        {
            case TileType.GROUND:
                return _groundTilemap.GetCellCenterWorld(new Vector3Int(gridpos.x, gridpos.y, 0));
            case TileType.FLOOR:
                return _floorTilemap.GetCellCenterWorld(new Vector3Int(gridpos.x, gridpos.y, 0));
            case TileType.WATER:
                return _waterTilemap.GetCellCenterWorld(new Vector3Int(gridpos.x, gridpos.y, 0));
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
        }
    }

    /// <summary>
    /// Convertit une position mondiale en position de grille.
    /// </summary>
    /// <param name="worldPose">La position dans l'espace mondial.</param>
    /// <returns>La position correspondante dans la grille.</returns>
    public Vector2 WorldToGridPos(Vector2 worldPose)
    {
        Vector3Int cellPos = _groundTilemap.WorldToCell(worldPose);
        return new Vector2(cellPos.x, cellPos.y);

    }
    private RuleTile GetRuleTile(BiomeType biome, TileType type)
    {
        if(_ruleTileCache.TryGetValue((biome, type), out RuleTile tile))
        {
            return tile;
        }
        return null; // Si aucune tuile n'est configurée
    }
    
    private void SetupBiomes()
    {
        float biomeCenterX = 0;
        float biomeCenterY = 0;
        Array allBiomeTypes = System.Enum.GetValues(typeof(BiomeType));
        List<BiomeType> validBiomes = new List<BiomeType>();

        foreach (BiomeType b in allBiomeTypes) {
            if (b != BiomeType.WATER) validBiomes.Add(b);
        }

        Shuffle(validBiomes);
        int totalBiomes = validBiomes.Count;
        _cols = Mathf.CeilToInt(Mathf.Sqrt(totalBiomes));
        _rows = Mathf.CeilToInt((float)totalBiomes / _cols);
        
        float secteurWidth = (float)_worldGenConfig.Size / _cols;
        float secteurHeight = (float)_worldGenConfig.Size / _rows;
        
        int biomeIndex = 0;
        for (int r = 0; r < _rows; r++) {
            for (int c = 0; c < _cols; c++) {
                if (biomeIndex >= totalBiomes) break; 

                float xMin = c * secteurWidth;
                float xMax = (c + 1) * secteurWidth;
                float yMin = r * secteurHeight;
                float yMax = (r + 1) * secteurHeight;
                biomeCenterX = Random.Range(xMin + _margeX, xMax - _margeX);
                biomeCenterY = Random.Range(yMin + _margeY, yMax - _margeY);
                _biomes.Add(new Biome(new Vector2(biomeCenterX, biomeCenterY), validBiomes[biomeIndex]));
                biomeIndex++;
            }
        }
    }
    
    private void Shuffle<T>(List<T> list)
    {
        for (int i = 0; i < list.Count; i++)
        {
            int index = Random.Range(0, i);
            (list[i], list[index]) = (list[index], list[i]);
        }
    }

    private void SetupThresholds(Dictionary<float, TileType> tilesTypeThresholds)
    {
        for (int j = 0; j <= sizeof(TileType) + 1; j++)
        {
            int type = j - 1;
            tilesTypeThresholds.Add(1f/j, (TileType) type);
        }
    }

    private TileData ComputeTile(Vector2Int position, float baseNoise)
    {
        Biome localBiome = GetClosestBiome(position);
        float distToCenter = Vector2.Distance(position, localBiome.Position);
        float maxIslandRadius = (_worldGenConfig.Size / (float)Mathf.Max(_rows, _cols)) * _worldGenConfig.IslandRadiusFactor;
        float islandFalloff = Mathf.Clamp01(distToCenter / maxIslandRadius);
        
        float finalHeight = baseNoise * (1 - islandFalloff);
        
        if(finalHeight < _worldGenConfig.SeaLevel)
        {
            return new TileData(position, BiomeType.WATER, TileType.WATER, 0, finalHeight);
        }
        if(finalHeight > _worldGenConfig.CliffLevel)
        {
            return new TileData(position, localBiome.Type, TileType.FLOOR, 0, finalHeight);
        }
        return new TileData(position, localBiome.Type, TileType.GROUND, 0, finalHeight);
    }

    private void SmoothCliffs(int iterations)
    {
        int size = _worldGenConfig.Size;
        TileType[,] nextTypes = new TileType[size, size];
        
        for (int i = 0; i < iterations; i++)
        {
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    nextTypes[x, y] = _grid[x, y].Type;
                    if (nextTypes[x, y] == TileType.WATER) continue;
                    
                    int n = GetNeighbours(x, y, TileType.FLOOR);
                    
                    if(n > _worldGenConfig.FloorNeighboursRequired) nextTypes[x, y] = TileType.FLOOR;
                    else if(n < _worldGenConfig.FloorNeighboursRequired) nextTypes[x, y] = TileType.GROUND;
                }
            }
            
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    _grid[x, y].Type = nextTypes[x, y];
                }
            }
        }
    }

    private int GetNeighbours(int x, int y, TileType type)
    {
        int count = 0;
        for (int dx = -1; dx <= 1; dx++)
        for (int dy = -1; dy <= 1; dy++)
        {
            if (dx == 0 && dy == 0) continue;
            int nx = x + dx, ny = y + dy;
            if (nx >= 0 && nx < _worldGenConfig.Size && ny >= 0 && ny < _worldGenConfig.Size)
                if (_grid[nx, ny].Type == type) count++;
        }
        return count;
    }
    private void ComputeBiomeMap()
    {
        int size = _worldGenConfig.Size;
        _biomeMap = new Biome[size, size];

        for (int x = 0; x < size; x++)
        {
            for (int y = 0; y < size; y++)
            {
                Vector2 pos = new Vector2(x, y);
                Biome closestBiome = _biomes[0];
                float minDistSq = Mathf.Infinity;

                foreach (Biome b in _biomes)
                {
                    float distSq = (pos - b.Position).sqrMagnitude;
                    if (distSq < minDistSq)
                    {
                        minDistSq = distSq;
                        closestBiome = b;
                    }
                }
                _biomeMap[x, y] = closestBiome;
            }
        }
    }

    private Biome GetClosestBiome(Vector2 position)
    {
        if (_biomeMap != null)
        {
            int x = Mathf.Clamp(Mathf.RoundToInt(position.x), 0, _worldGenConfig.Size - 1);
            int y = Mathf.Clamp(Mathf.RoundToInt(position.y), 0, _worldGenConfig.Size - 1);
            return _biomeMap[x, y];
        }

        Biome closestBiome = _biomes[0];
        float minDistSq = Mathf.Infinity;

        foreach (Biome b in _biomes)
        {
            float distSq = (position - b.Position).sqrMagnitude;
            if (distSq < minDistSq)
            {
                minDistSq = distSq;
                closestBiome = b;
            }
        }

        return closestBiome;
    }

    private void GenerateLakes()
    {
        foreach (Biome biome in _biomes)
        {
            int lakesCreated = 0;
            int attempts = 0;
            
            while (lakesCreated < _worldGenConfig.MaxLakesPerBiome && attempts < 100)
            {
                attempts++;
                int startX = Random.Range(1, _worldGenConfig.Size - 1);
                int startY = Random.Range(1, _worldGenConfig.Size - 1);

                // Si on a trouvé un bout de terre de ce biome, on creuse un lac
                if (_grid[startX, startY].Type == TileType.GROUND && _grid[startX, startY].Biome == biome.Type)
                {
                    CarveLake(startX, startY);
                    lakesCreated++;
                }
            }
        }
    }

    private void CarveLake(int startX, int startY)
    {
        int targetSize = Random.Range(_worldGenConfig.MaxLakeSize / 2, _worldGenConfig.MaxLakeSize);
        int minViableSize = _worldGenConfig.MinLakeSize;

        List<Vector2Int> carvedTiles = new List<Vector2Int>();
        List<Vector2Int> frontier = new List<Vector2Int> { new Vector2Int(startX, startY) };

        while (carvedTiles.Count < targetSize && frontier.Count > 0) //Flood filled
        {
            int randomIndex = Random.Range(0, frontier.Count);
            Vector2Int pos = frontier[randomIndex];
            frontier.RemoveAt(randomIndex);

            if (pos.x < 0 || pos.x >= _worldGenConfig.Size || pos.y < 0 || pos.y >= _worldGenConfig.Size)
                continue;

            if (_grid[pos.x, pos.y].Type != TileType.GROUND)
                continue;

            // On ne change que le Type : Height et Biome restent intacts
            _grid[pos.x, pos.y].Type = TileType.WATER;
            carvedTiles.Add(pos);

            // On ajoute les voisins pour la prochaine itération
            frontier.Add(new Vector2Int(pos.x + 1, pos.y));
            frontier.Add(new Vector2Int(pos.x - 1, pos.y));
            frontier.Add(new Vector2Int(pos.x, pos.y + 1));
            frontier.Add(new Vector2Int(pos.x, pos.y - 1));
        }

        // Un lac trop petit est disgracieux : on annule plutôt que de laisser une flaque
        if (carvedTiles.Count < minViableSize)
        {
            foreach (Vector2Int pos in carvedTiles)
                _grid[pos.x, pos.y].Type = TileType.GROUND;
        }
    }

    /// <summary>
    /// Supprime les petites poches d'eau parasites (produites par le bruit dans ComputeTile).
    /// On regroupe l'eau en composantes connexes : on garde la mer (touche le bord) et les
    /// vrais lacs (>= MinLakeSize) ; le reste est reconverti en terre (biome local + type selon Height).
    /// </summary>
    private void RemoveSmallWaterBodies()
    {
        int size = _worldGenConfig.Size;
        bool[,] visited = new bool[size, size];
        Vector2Int[] dirs = { Vector2Int.up, Vector2Int.down, Vector2Int.left, Vector2Int.right };

        for (int y = 0; y < size; y++)
        for (int x = 0; x < size; x++)
        {
            if (_grid[x, y].Type != TileType.WATER || visited[x, y]) continue;

            // BFS pour collecter toute la composante d'eau connexe (4-connexité).
            List<Vector2Int> component = new List<Vector2Int>();
            Queue<Vector2Int> queue = new Queue<Vector2Int>();
            queue.Enqueue(new Vector2Int(x, y));
            visited[x, y] = true;
            bool touchesBorder = false;

            while (queue.Count > 0)
            {
                Vector2Int cur = queue.Dequeue();
                component.Add(cur);

                if (cur.x == 0 || cur.x == size - 1 || cur.y == 0 || cur.y == size - 1)
                    touchesBorder = true;

                foreach (Vector2Int d in dirs)
                {
                    int nx = cur.x + d.x, ny = cur.y + d.y;
                    if (nx < 0 || nx >= size || ny < 0 || ny >= size) continue;
                    if (visited[nx, ny] || _grid[nx, ny].Type != TileType.WATER) continue;
                    visited[nx, ny] = true;
                    queue.Enqueue(new Vector2Int(nx, ny));
                }
            }

            // Mer (touche le bord) ou vrai lac (assez grand) : on garde.
            if (touchesBorder || component.Count >= _worldGenConfig.MinLakeSize) continue;

            // Sinon : flaque parasite -> retour à la terre.
            foreach (Vector2Int cell in component)
            {
                _grid[cell.x, cell.y].Biome = GetClosestBiome(new Vector2(cell.x, cell.y)).Type;
                _grid[cell.x, cell.y].Type = _grid[cell.x, cell.y].Height > _worldGenConfig.CliffLevel
                    ? TileType.FLOOR
                    : TileType.GROUND;
            }
        }
    }

    private void GenerateRivers()
    {
        foreach (Biome biome in _biomes)
        {
            List<Vector2Int> possibleSources = new List<Vector2Int>();
            for (int y = 1; y < _worldGenConfig.Size - 1; y++)
            {
                for (int x = 1; x < _worldGenConfig.Size - 1; x++)
                {
                    // On ne garde que les FLOOR en bordure de plateau (au moins un voisin GROUND) :
                    // une source en plein centre d'un plateau n'a pas de vraie pente à suivre.
                    if (_grid[x, y].Type == TileType.FLOOR && _grid[x, y].Biome == biome.Type
                        && GetNeighbours(x, y, TileType.GROUND) > 0)
                    {
                        possibleSources.Add(new Vector2Int(x, y));
                    }
                }
            }

            if (possibleSources.Count == 0) continue;

            int numRivers = Random.Range(0, _worldGenConfig.MaxRiversPerBiome + 1);
            int riversPlaced = 0;
            int attempts = 0;
            int maxAttempts = possibleSources.Count * 2;

            // On ré-essaie avec d'autres sources : une rivière n'est retenue que si
            // son tracé atteint réellement de l'eau (mer, lac) ou le bord de la carte.
            while (riversPlaced < numRivers && attempts < maxAttempts)
            {
                attempts++;
                Vector2Int source = possibleSources[Random.Range(0, possibleSources.Count)];

                List<Vector2Int> path = new List<Vector2Int>();
                if (TryTraceRiver(source.x, source.y, path))
                {
                    CarveRiverPath(path);
                    riversPlaced++;
                }
            }

            Debug.Log($"[Rivers] Biome {biome.Type} : {riversPlaced}/{numRivers} rivière(s) placée(s) en {attempts} essais ({possibleSources.Count} sources).");
        }
    }

    private void CarveRiver(int x, int y)
    {
        Vector2 currentPos = new Vector2(x, y);

        //angle aleatoiure
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        int currentLength = 0;

        while (currentLength < _worldGenConfig.MaxRiverLength)
        {
            int cx = Mathf.RoundToInt(currentPos.x);
            int cy = Mathf.RoundToInt(currentPos.y);
            
            for (int tx = -_worldGenConfig.MaxRiverThickness; tx <= _worldGenConfig.MaxRiverThickness; tx++)
            {
                for (int ty = -_worldGenConfig.MaxRiverThickness; ty <= _worldGenConfig.MaxRiverThickness; ty++)
                {
                    if (tx * tx + ty * ty <= _worldGenConfig.MaxRiverThickness * _worldGenConfig.MaxRiverThickness)  //si on est dans le cercle
                    {
                        int carveX = cx + tx;
                        int carveY = cy + ty;

                        if (carveX >= 0 && carveX < _worldGenConfig.Size && carveY >= 0 && carveY < _worldGenConfig.Size)
                        {
                            // On creuse si ce n'est pas une montagne/étage et que ce n'est pas DÉJÀ de l'eau
                            if (_grid[carveX, carveY].Type != TileType.FLOOR && _grid[carveX, carveY].Type != TileType.WATER) 
                            {
                                _grid[carveX, carveY] = new TileData(new Vector2Int(carveX, carveY), BiomeType.WATER, TileType.WATER);
                            }
                        }
                    }
                }
            }
            
            angle += Random.Range(-0.25f, 0.25f); 
            direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            
            currentPos += direction;
            currentLength++;
            
            int forwardX = Mathf.RoundToInt(currentPos.x + direction.x * (_worldGenConfig.MaxRiverThickness + 2));
            int forwardY = Mathf.RoundToInt(currentPos.y + direction.y * (_worldGenConfig.MaxRiverThickness + 2));
            
            if (forwardX >= 0 && forwardX < _worldGenConfig.Size && forwardY >= 0 && forwardY < _worldGenConfig.Size)
            {
                if (_grid[forwardX, forwardY].Type == TileType.WATER) break; 
            }
        }
    }

    /// <summary>
    /// Simule le trajet d'une goutte depuis (startX, startY) par descente de gradient + inertie,
    /// SANS rien creuser. Remplit 'path' avec les cellules terrestres traversées.
    /// Retourne true si la goutte atteint de l'eau (mer/lac) ou le bord de carte (océan),
    /// false si elle s'arrête en pleine terre (MaxRiverLength) : dans ce cas la rivière ne relie rien.
    /// </summary>
    private bool TryTraceRiver(int startX, int startY, List<Vector2Int> path)
    {
        Vector2 pos = new Vector2(startX, startY);
        Vector2 dir = Vector2.zero;
        float inertia = _worldGenConfig.RiverInertia;

        for (int i = 0; i < _worldGenConfig.MaxRiverLength; i++)
        {
            int cx = Mathf.RoundToInt(pos.x);
            int cy = Mathf.RoundToInt(pos.y);

            // Gradient de MONTÉE du terrain. La descente est donc -gradient.
            Vector2 gradient = HeightGradient(cx, cy);

            // On mélange l'élan précédent (inertie) avec la direction de descente.
            dir = dir * inertia - gradient * (1f - inertia);

            // Terrain plat + aucun élan : petite poussée aléatoire pour ne pas rester bloqué.
            if (dir.sqrMagnitude < 1e-6f)
            {
                float a = Random.value * Mathf.PI * 2f;
                dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            }
            dir.Normalize();

            pos += dir;

            int nx = Mathf.RoundToInt(pos.x);
            int ny = Mathf.RoundToInt(pos.y);

            // Atteint le bord : l'océan entoure l'île -> la rivière se jette dans la mer.
            if (nx < 0 || nx >= _worldGenConfig.Size || ny < 0 || ny >= _worldGenConfig.Size)
                return true;

            // Atteint de l'eau pré-existante (mer, lac, autre rivière) -> connexion réussie.
            if (_grid[nx, ny].Type == TileType.WATER)
                return true;

            path.Add(new Vector2Int(nx, ny));
        }

        // Longueur max atteinte sans jamais rencontrer d'eau : la rivière ne relie rien.
        return false;
    }

    /// <summary>
    /// Creuse le tracé validé d'une rivière, avec largeur (MaxRiverThickness comme rayon)
    /// et cellules-pont sur les pas diagonaux pour garder l'eau connectée orthogonalement.
    /// </summary>
    private void CarveRiverPath(List<Vector2Int> path)
    {
        int radius = _worldGenConfig.MaxRiverThickness;

        for (int i = 0; i < path.Count; i++)
        {
            Vector2Int p = path[i];

            if (i > 0)
            {
                Vector2Int prev = path[i - 1];
                if (p.x != prev.x && p.y != prev.y)
                    CarveCell(p.x, prev.y); // pont anti-diagonale
            }

            CarveRiverPoint(p.x, p.y, radius);
        }
    }

    /// <summary>
    /// Gradient de hauteur (direction de MONTÉE) par différences finies centrées.
    /// </summary>
    private Vector2 HeightGradient(int x, int y)
    {
        float hL = HeightAtClamped(x - 1, y);
        float hR = HeightAtClamped(x + 1, y);
        float hD = HeightAtClamped(x, y - 1);
        float hU = HeightAtClamped(x, y + 1);
        return new Vector2((hR - hL) * 0.5f, (hU - hD) * 0.5f);
    }

    private float HeightAtClamped(int x, int y)
    {
        x = Mathf.Clamp(x, 0, _worldGenConfig.Size - 1);
        y = Mathf.Clamp(y, 0, _worldGenConfig.Size - 1);
        return _grid[x, y].Height;
    }

    /// <summary>
    /// Creuse un disque d'eau de rayon donné (0 = 1 cellule, 1 = 3 cases de large, etc.).
    /// </summary>
    private void CarveRiverPoint(int centerX, int centerY, int radius)
    {
        for (int tx = -radius; tx <= radius; tx++)
        for (int ty = -radius; ty <= radius; ty++)
        {
            if (tx * tx + ty * ty > radius * radius) continue;
            CarveCell(centerX + tx, centerY + ty);
        }
    }

    /// <summary>
    /// Transforme une cellule en eau si elle est valide (dans la grille, pas une falaise),
    /// en préservant Height et Biome.
    /// </summary>
    private void CarveCell(int x, int y)
    {
        if (x < 0 || x >= _worldGenConfig.Size || y < 0 || y >= _worldGenConfig.Size) return;
        if (_grid[x, y].Type == TileType.FLOOR) return;
        _grid[x, y].Type = TileType.WATER;
    }
}

