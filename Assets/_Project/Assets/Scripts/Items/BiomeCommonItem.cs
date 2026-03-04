using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "BiomeCommonItem", menuName = "Scriptable Objects/BiomeCommonItem")]
public class BiomeCommonItem : ScriptableObject
{
    public BiomeType Biome;
    public List<TileBase> ItemTiles;
}
