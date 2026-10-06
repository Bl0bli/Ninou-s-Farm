using System;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Grille de test : construit les données logiques à partir de tuiles peintes à la main,
/// au lieu de générer un monde procédural. Utilisée par la scène Test_Tools.
/// Convention : la carte est peinte à partir de la case (0,0), en coordonnées positives.
/// Une case avec une tuile FLOOR est une falaise, une case avec une tuile GROUND est du sol,
/// toute autre case est de l'eau.
/// </summary>
public class TestTileGrid : MonoBehaviour, ITileGrid
{
    [SerializeField] private Tilemap _groundTilemap;
    [SerializeField] private Tilemap _floorTilemap;
    [SerializeField] private BiomeType _biome = BiomeType.HILLS;

    private TileGridData _data;

    public event Action<Vector2Int> OnTileChanged;

    private void Awake()
    {
        _data = BuildFromTilemaps();
        _data.OnTileChanged += cell => OnTileChanged?.Invoke(cell);
        TileGridLocator.Register(this);
    }

    private void OnDestroy()
    {
        TileGridLocator.Unregister(this);
    }

    private TileGridData BuildFromTilemaps()
    {
        _groundTilemap.CompressBounds();
        BoundsInt bounds = _groundTilemap.cellBounds;

        int width = Mathf.Max(0, bounds.xMax);
        int height = Mathf.Max(0, bounds.yMax);
        TileData[,] tiles = new TileData[width, height];

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                Vector3Int cell = new Vector3Int(x, y, 0);
                TileType type = ReadTileType(cell);
                BiomeType biome = type == TileType.WATER ? BiomeType.WATER : _biome;
                tiles[x, y] = new TileData(new Vector2Int(x, y), biome, type);
            }
        }

        return new TileGridData(tiles);
    }

    private TileType ReadTileType(Vector3Int cell)
    {
        if (_floorTilemap != null && _floorTilemap.HasTile(cell)) return TileType.FLOOR;
        if (_groundTilemap.HasTile(cell)) return TileType.GROUND;
        return TileType.WATER;
    }

    public Vector2Int WorldToGrid(Vector2 worldPosition)
    {
        Vector3Int cell = _groundTilemap.WorldToCell(worldPosition);
        return new Vector2Int(cell.x, cell.y);
    }

    public TileData GetTileAt(Vector2Int gridPosition) => _data.GetTileAt(gridPosition);
    public bool CanBeFarmed(Vector2Int gridPosition) => _data.CanBeFarmed(gridPosition);

    public bool SetTileState(Vector2Int gridPosition, TileState state) => _data.SetTileState(gridPosition, state);
    public bool SetTileWatered(Vector2Int gridPosition, bool isWatered) => _data.SetTileWatered(gridPosition, isWatered);
}
