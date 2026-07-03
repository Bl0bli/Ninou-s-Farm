using UnityEngine;

[CreateAssetMenu(fileName = "SO_WorldGenConfig", menuName = "Scriptable Objects/SO_WorldGenConfig")]
public class SO_WorldGenConfig : ScriptableObject
{
    [Header("Seed Params")]
    public int Seed;
    public bool UseRandomSeed = true;
    
    [Header("Ground Params")] 
    public int Size = 100;
    public float Scale = 0.1f;
    public float MixScale = 0.4f;

    [Header("Island Params")] 
    public float IslandRadiusFactor = 0.7f;
    public int Octaves = 4;
    public float Lacunarity = 2f;
    public float Persistence = 0.5f;
    public int CliffSmoothingIterations = 3;
    public int FloorNeighboursRequired = 4;

    [Header("Terrain Thresholds")] 
    [Range(0, 1)] public float SeaLevel = 0.3f;
    [Range(0, 1)] public float CliffLevel = 0.8f;
    
    
    [Header("Slopes Generation Params")] 
    public int HorizontalDistBetweenSlopes = 8;
    public int MaxSlopesPerHill = 3;
    
    [Header("Lakes & Rivers Params")]
    public int MaxLakesPerBiome = 3;
    public int MaxLakeSize = 25; // Nombre de cases max par lac
    public int MinLakeSize = 8;  // En dessous, le lac est annulé (évite les flaques d'1-2 cases)
    public int MaxRiversPerBiome = 2;
    public int MaxRiverLength = 40;
    public int MaxRiverThickness = 2;
    [Range(0f, 1f)] public float RiverInertia = 0.5f; // 0 = suit strictement la pente (se coince), 1 = ligne droite
}
