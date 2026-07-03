using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_BiomeConfig", menuName = "Scriptable Objects/SO_BiomeConfig")]
public class SO_BiomeConfig : ScriptableObject
{
    public BiomeType Biome;
    public List<Sprite> GroundSprites; 
    public List<Sprite> FloorSprites;
}
