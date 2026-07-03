using System;
using System.Collections.Generic;
using UnityEngine;

public class WorldState : MonoBehaviour
{
    [SerializeField] private GameObject _prefabTile;

    [SerializeField] private List<SO_BiomeConfig> _biomes;
    
    private Dictionary<BiomeType, SO_BiomeConfig> _biomeConfigMap;
    private bool _initialized = false;

    public static WorldState Instance;
    private void Awake()
    {
        if(Instance != null) Destroy(this);
        Instance = this;
        
        if(!_initialized)   InitBiomeConfigMap();
    }

    /// <summary>
    /// Crée une tuile visuelle dans le monde à partir des données de tuile fournies.
    /// </summary>
    /// <param name="tileData">Les données de la tuile à instancier.</param>
    public void CreateTile(TileData tileData)
    {
        GameObject go = Instantiate(_prefabTile);
        go.transform.position = new Vector2(tileData.Position.x  * 0.16f, tileData.Position.y * 0.16f);
        WorldTile tile = go.GetComponent<WorldTile>();
        tile.SetSprite(GetSpriteForBiomeByBitMask(tileData.Biome, tileData.Mask, tileData.Type));
    }

    private Sprite GetSpriteForBiomeByBitMask(BiomeType biome, int bitMask, TileType type)
    {
        if (!_initialized) InitBiomeConfigMap();
        if (type == TileType.FLOOR) return _biomeConfigMap[biome].FloorSprites[bitMask];
        
        return _biomeConfigMap[biome].GroundSprites[bitMask];
    }

    private void InitBiomeConfigMap()
    {
        _biomeConfigMap = new Dictionary<BiomeType, SO_BiomeConfig>();
        foreach (SO_BiomeConfig biomeConfig in _biomes)
        {
            _biomeConfigMap[biomeConfig.Biome] = biomeConfig;
        }

        _initialized = true;
    }
}
