using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Mathematics;
using UnityEngine;
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
    [SerializeField] private Camera _camera;
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
    [SerializeField] private int _size = 100;
    [SerializeField] private float _scale = 0.1f;
    [SerializeField] private float _mixScale = 0.4f;
    [SerializeField] private float _waterPercentage = 0.45f;
    [SerializeField] private float _wallPercentage = 0.1f;
    [SerializeField] private float _margeX;
    [SerializeField] private float _margeY;
    [SerializeField, Range(1f, 10f)] private float _falloffPower = 3f;
    [SerializeField, Range(1f, 5f)] private float _falloffRange = 2.2f;

    [Header("Slopes Generation Params")] 
    [SerializeField] private int _horizontalDistBetweenSlopes = 8;
    [SerializeField] private int _maxSlopesPerHill = 3;
    
    [Header("Lakes & Rivers Params")]
    [SerializeField] private int _maxLakesPerBiome = 3;
    [SerializeField] private int _maxLakeSize = 25; // Nombre de cases max par lac
    [SerializeField] private int _maxRiversPerBiome = 2;
    [SerializeField] private int _maxRiverLength = 40;
    [SerializeField] private int _maxRiverThickness = 2;
    
    private TileData[,] _grid;
    private List<Biome> _biomes = new List<Biome>();
    private Dictionary<(BiomeType, TileType), RuleTile> _ruleTileCache;
    
    private int _rows, _cols;

    public TileData GetTileAt(Vector2Int gridPos) => _grid[gridPos.x, gridPos.y];

    public TileData GetTileAt(Vector3 worldPos)
    {
        //Debug.Log(dir);
        Vector3Int cellPos = _groundTilemap.WorldToCell(worldPos);
        if (cellPos.x >= 0 && cellPos.x < _size && cellPos.y >= 0 && cellPos.y < _size)
            return _grid[cellPos.x, cellPos.y];
        return new TileData(new Vector2Int(cellPos.x, cellPos.y), BiomeType.WATER, TileType.WATER);
    }
    public int GridSize => _size;
    public static GridWorld Instance;
    public event Action<Vector2> OnWorldInit;
    private void Awake()
    {
        if(Instance != null) Destroy(this);
        Instance = this;
    }

    private void Start()
    {
        _camera.transform.position = new Vector3(_size / 2, _size / 2, -10);
        _camera.orthographicSize = _size / 2;
        _ruleTileCache = new Dictionary<(BiomeType, TileType), RuleTile>();
        foreach (BiomeRuleTileMapping mapping in _tileMappings)
        {
            _ruleTileCache[(mapping.Biome, mapping.Type)] = mapping.RuleTile;
        }
        GameManager.Instance.Init();

        StartCoroutine(GenerateWorldRoutine());
    }

    IEnumerator GenerateWorldRoutine()
    {
        Dictionary<float, TileType> tilesTypeThresholds = new Dictionary<float, TileType>();
        SetupThresholds(tilesTypeThresholds);
        
        float xoffset = Random.Range(-1000, 1000);
        float yOffset = Random.Range(-1000, 1000);
        _grid = new TileData[_size, _size];

        SetupBiomes();

        yield return null;
        
        float[,] noiseMap = new float[_size, _size];
        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                noiseMap[x, y] = Mathf.PerlinNoise(xoffset + x * _scale, yOffset + y * _scale);
            }
        }

        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                // On passe uniquement la position et le bruit brut, la fonction s'occupe du reste
                TileData tileData = ComputeTile(new Vector2Int(x, y), noiseMap[x, y]);
                _grid[x, y] = tileData;
            }
        }

        yield return null;
        GenerateLakes();
        yield return null;
        GenerateRivers();
        yield return null;
        yield return StartCoroutine(CollapseTilesRoutine());
        _itemsGeneration.Init();
        OnWorldInit?.Invoke(GetSpawnableTile());
    }

    private IEnumerator CollapseTilesRoutine()
    {
        RuleTile waterTile = GetRuleTile(BiomeType.WATER, TileType.WATER);

        int tilesDrawn = 0;

        current = _MAXtilesPerFrame;
        
        TilemapCollider2D collisionTilemapCollider = _collisionTilemap.GetComponent<TilemapCollider2D>();
        TilemapCollider2D groundTilemapCollider = _groundTilemap.GetComponent<TilemapCollider2D>();
        
        if (collisionTilemapCollider != null) collisionTilemapCollider.enabled = false;
        if (groundTilemapCollider != null) groundTilemapCollider.enabled = false;

        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
            {
                TileData tileData = _grid[x, y];
                Vector3Int pos = new Vector3Int(x, y, 0);
                
                if (waterTile != null)
                {
                    _waterTilemap.SetTile(pos, waterTile);
                }

                if (tileData.Type == TileType.WATER)
                {
                    if(waterTile != null) _collisionTilemap.SetTile(pos, waterTile);
                }
                if (tileData.Type == TileType.GROUND || tileData.Type == TileType.FLOOR)
                {
                    RuleTile groundTile = GetRuleTile(tileData.Biome, TileType.GROUND);
                    if (groundTile != null)
                    {
                        _groundTilemap.SetTile(pos, groundTile);
                    }
                }
                if (tileData.Type == TileType.FLOOR)
                {
                    RuleTile floorTile = GetRuleTile(tileData.Biome, TileType.FLOOR);
                    if (floorTile != null)
                    {
                        _floorTilemap.SetTile(pos, floorTile);
                    }
                }

                tilesDrawn++;
                if (tilesDrawn >= current)
                {
                    tilesDrawn = 0;
                    yield return null;

                    currentFPS = 1f / Time.smoothDeltaTime;

                    float t = Mathf.InverseLerp(_minFPS, _maxFPS, currentFPS);

                    current = Mathf.RoundToInt(Mathf.Lerp(_MINtilesPerFrame, _MAXtilesPerFrame, t));
                }
            }
        }

        yield return null;
        
        if (collisionTilemapCollider != null) collisionTilemapCollider.enabled = true;
        if (groundTilemapCollider != null) groundTilemapCollider.enabled = true;
        
        bool[,] visited = new bool[_size, _size];
        for (int x = 0; x < _size; x++)
        {
            for (int y = 0; y < _size; y++)
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
                        if (currentTile.x - 1 >= 0 && currentTile.x + 2 < _size && currentTile.y - 1 >= 0)                        
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
                        int slopesToPlace = Mathf.Clamp(potentialSlopes.Count / _horizontalDistBetweenSlopes, 1, _maxSlopesPerHill);
                        CollapseTilesToSlopes(potentialSlopes, slopesToPlace, tile.Biome);
                    }
                }
            }
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
            if (pos.x >= 0 && pos.x < _size && pos.y >= 0 && pos.y < _size)
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
        for (int y = 0; y < _size; y++)
        {
            for (int x = 0; x < _size; x++)
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
        
        Vector3 defaultWorldPos = _groundTilemap.GetCellCenterWorld(new Vector3Int(_size / 2, _size / 2, 0));
        return new Vector2(defaultWorldPos.x, defaultWorldPos.y);
    }
    
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
        
        float secteurWidth = (float)_size / _cols;
        float secteurHeight = (float)_size / _rows;
        
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
        float maxIslandRadius = (_size / (float)Mathf.Max(_rows, _cols)) * 0.7f;
        float islandFalloff = Mathf.Clamp01(distToCenter / maxIslandRadius);
        
        if (islandFalloff > 0.75f + (baseNoise * 0.2f)) 
        {
            return new TileData(position, BiomeType.WATER, TileType.WATER);
        }
        
        TileType type = TileType.GROUND;
        
        if (baseNoise > (1 - _wallPercentage) && islandFalloff < 0.5f) 
        {
            type = TileType.FLOOR;
        }

        return new TileData(position, localBiome.Type, type);
    }
    private Biome GetClosestBiome(Vector2 position)
    {
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
            
            while (lakesCreated < _maxLakesPerBiome && attempts < 100)
            {
                attempts++;
                int startX = Random.Range(1, _size - 1);
                int startY = Random.Range(1, _size - 1);

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
        int targetSize = Random.Range(_maxLakeSize / 2, _maxLakeSize);
        List<Vector2Int> currentLakeTiles = new List<Vector2Int> { new Vector2Int(startX, startY) };
        int currentSize = 0;
        
        while (currentSize < targetSize && currentLakeTiles.Count > 0) //Flood filled
        {
            int randomIndex = Random.Range(0, currentLakeTiles.Count);
            Vector2Int pos = currentLakeTiles[randomIndex];
            currentLakeTiles.RemoveAt(randomIndex);

            if (pos.x >= 0 && pos.x < _size && pos.y >= 0 && pos.y < _size)
            {
                if (_grid[pos.x, pos.y].Type == TileType.GROUND)
                {
                    // On transforme la terre en eau
                    _grid[pos.x, pos.y] = new TileData(pos, BiomeType.WATER, TileType.WATER);
                    currentSize++;

                    // On ajoute les voisins pour la prochaine itération
                    currentLakeTiles.Add(new Vector2Int(pos.x + 1, pos.y));
                    currentLakeTiles.Add(new Vector2Int(pos.x - 1, pos.y));
                    currentLakeTiles.Add(new Vector2Int(pos.x, pos.y + 1));
                    currentLakeTiles.Add(new Vector2Int(pos.x, pos.y - 1));
                }
            }
        }
    }

    private void GenerateRivers()
    {
        foreach (Biome biome in _biomes)
        {
            for (int i = 0; i < _maxRiversPerBiome; i++)
            {
                int startX = Random.Range(1, _size - 1);
                int startY = Random.Range(1, _size - 1);

                if (_grid[startX, startY].Type == TileType.GROUND && _grid[startX, startY].Biome == biome.Type)
                {
                    CarveRiver(startX, startY);
                }
            }
        }
    }

    private void CarveRiver(int x, int y)
    {
        Vector2 currentPos = new Vector2(x, y);

        //angle aleatoiure
        float angle = Random.Range(0f, 360f) * Mathf.Deg2Rad;
        Vector2 direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));

        int currentLength = 0;

        while (currentLength < _maxRiverLength)
        {
            int cx = Mathf.RoundToInt(currentPos.x);
            int cy = Mathf.RoundToInt(currentPos.y);
            
            for (int tx = -_maxRiverThickness; tx <= _maxRiverThickness; tx++)
            {
                for (int ty = -_maxRiverThickness; ty <= _maxRiverThickness; ty++)
                {
                    if (tx * tx + ty * ty <= _maxRiverThickness * _maxRiverThickness)  //si on est dans le cercle
                    {
                        int carveX = cx + tx;
                        int carveY = cy + ty;

                        if (carveX >= 0 && carveX < _size && carveY >= 0 && carveY < _size)
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
            
            int forwardX = Mathf.RoundToInt(currentPos.x + direction.x * (_maxRiverThickness + 2));
            int forwardY = Mathf.RoundToInt(currentPos.y + direction.y * (_maxRiverThickness + 2));
            
            if (forwardX >= 0 && forwardX < _size && forwardY >= 0 && forwardY < _size)
            {
                if (_grid[forwardX, forwardY].Type == TileType.WATER) break; 
            }
        }
    }
}
