using Godot;

/// <summary>
/// Réglages de la génération du monde (port de SO_WorldGenConfig.cs). Mêmes noms de champs que l'asset Unity :
/// resources/world/world_gen_config.tres en est la copie générée.
/// </summary>
[GlobalClass]
public partial class WorldGenConfig : Resource
{
    [ExportGroup("Seed Params")]
    [Export] public int Seed;
    [Export] public bool UseRandomSeed = true;

    [ExportGroup("Ground Params")]
    [Export] public int Size = 100;
    [Export] public float Scale = 0.1f;
    [Export] public float MixScale = 0.4f;

    [ExportGroup("Island Params")]
    [Export] public float IslandRadiusFactor = 0.7f;
    [Export] public int Octaves = 4;
    [Export] public float Lacunarity = 2f;
    [Export] public float Persistence = 0.5f;
    [Export] public int CliffSmoothingIterations = 3;
    [Export] public int FloorNeighboursRequired = 4;

    [ExportGroup("Terrain Thresholds")]
    [Export(PropertyHint.Range, "0,1")] public float SeaLevel = 0.3f;
    [Export(PropertyHint.Range, "0,1")] public float CliffLevel = 0.8f;

    [ExportGroup("Slopes Generation Params")]
    [Export] public int HorizontalDistBetweenSlopes = 8;
    [Export] public int MaxSlopesPerHill = 3;

    [ExportGroup("Lakes & Rivers Params")]
    [Export] public int MaxLakesPerBiome = 3;
    /// <summary>Nombre de cases max par lac.</summary>
    [Export] public int MaxLakeSize = 25;
    /// <summary>En dessous, le lac est annulé (évite les flaques d'1-2 cases).</summary>
    [Export] public int MinLakeSize = 8;
    [Export] public int MaxRiversPerBiome = 2;
    [Export] public int MaxRiverLength = 40;
    [Export] public int MaxRiverThickness = 2;
    /// <summary>0 = suit strictement la pente (se coince), 1 = ligne droite.</summary>
    [Export(PropertyHint.Range, "0,1")] public float RiverInertia = 0.5f;
}
