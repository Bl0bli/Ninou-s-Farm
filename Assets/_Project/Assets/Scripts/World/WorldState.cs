using System;
using System.Collections.Generic;
using UnityEngine;

public class WorldState : MonoBehaviour
{
    [SerializeField] private GameObject _prefabTile;

    [Header("Biome sprites tiles")] 
    [SerializeField] private List<Sprite> _spriteTileHILL = new List<Sprite>();
    [SerializeField] private List<Sprite> _spriteTileFOREST = new List<Sprite>();
    [SerializeField] private List<Sprite> _spriteTileDESERT = new List<Sprite>();
    [SerializeField] private List<Sprite> _spriteTileDEADZONE = new List<Sprite>();
    [SerializeField] private List<Sprite> _spriteTileMOUNTAINS = new List<Sprite>();
    [SerializeField] private List<Sprite> _spriteTileWATER = new List<Sprite>();
    
    [Header("Biome sprites high tiles")] 
    [SerializeField] private List<Sprite> _spriteTileHighHILL = new List<Sprite>();
    [SerializeField] private List<Sprite> _spriteTileHighFOREST = new List<Sprite>();
    [SerializeField] private List<Sprite> _spriteTileHighDESERT = new List<Sprite>();
    [SerializeField] private List<Sprite> _spriteTileHighDEADZONE = new List<Sprite>();
    [SerializeField] private List<Sprite> _spriteTileHighMOUNTAINS = new List<Sprite>();

    public static WorldState Instance;
    private void Awake()
    {
        if(Instance != null) Destroy(this);
        Instance = this;
    }

    public void CreateTile(TileData tileData)
    {
        GameObject go = Instantiate(_prefabTile);
        go.transform.position = new Vector2(tileData.Position.x  * 0.16f, tileData.Position.y * 0.16f);
        WorldTile tile = go.GetComponent<WorldTile>();
        tile.SetSprite(GetSpriteForBiomeByBitMask(tileData.Biome, tileData.Mask, tileData.Type));
    }

    private Sprite GetSpriteForBiomeByBitMask(BiomeType biome, int bitMask, TileType type)
    {
        if (type == TileType.WATER)
        {
            return _spriteTileWATER[bitMask]; 
        }
        if (type == TileType.FLOOR)
        {
            switch (biome)
            {
                case BiomeType.HILLS:
                    return _spriteTileHILL[bitMask];
                case BiomeType.FOREST:
                    return _spriteTileFOREST[bitMask];
                case BiomeType.DESERT:
                    return _spriteTileDESERT[bitMask];
                case BiomeType.DEADZONE:
                    return _spriteTileDEADZONE[bitMask];
                case BiomeType.MOUNTAINS:
                    return _spriteTileMOUNTAINS[bitMask];
            }
        }
        switch (biome)
        {
            case BiomeType.HILLS:
                return _spriteTileHighHILL[bitMask];
            case BiomeType.FOREST:
                return _spriteTileHighFOREST[bitMask];
            case BiomeType.DESERT:
                return _spriteTileHighDESERT[bitMask];
            case BiomeType.DEADZONE:
                return _spriteTileHighDEADZONE[bitMask];
            case BiomeType.MOUNTAINS:
                return _spriteTileHighMOUNTAINS[bitMask];
        }
        return _spriteTileWATER[bitMask]; 
    }
}
