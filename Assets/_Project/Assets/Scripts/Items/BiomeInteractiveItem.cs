using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "BiomeInteractiveItem", menuName = "Scriptable Objects/BiomeInteractiveItems")]
public class BiomeInteractiveItem : ScriptableObject
{
    public BiomeType Biome;
    public List<GameObject> Prefabs;
}
