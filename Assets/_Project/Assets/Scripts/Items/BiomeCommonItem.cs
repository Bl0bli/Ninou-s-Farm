using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "BiomeCommonItem", menuName = "Scriptable Objects/BiomeCommonItems")]
public class BiomeCommonItem : ScriptableObject
{
    public BiomeType Biome;
    public List<CommonItem> ItemTiles;
}
