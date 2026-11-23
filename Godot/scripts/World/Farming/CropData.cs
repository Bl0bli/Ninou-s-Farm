using System;
using Godot;

/// <summary>
/// Données d'une culture (port de CropData.cs).
/// Le champ _seed d'Unity a disparu : la graine référençait la culture et la culture la graine,
/// une boucle que Godot refuse de charger. C'est la graine qui se donne à la culture au moment de planter.
/// </summary>
[GlobalClass]
public partial class CropData : Resource
{
    /// <summary>Scène instanciée à la plantation (le prefab CropInstance dans Unity).</summary>
    [Export] private PackedScene _scene;
    [Export] private string _cropName = "";
    [Export] private Texture2D _icon;
    /// <summary>Heures de jeu arrosées nécessaires pour atteindre le dernier stade.</summary>
    [Export] private int _growTime;
    [Export] private Texture2D[] _growStages = Array.Empty<Texture2D>();
    [Export] private ItemData _loot;

    public string CropName => _cropName;
    public Texture2D Icon => _icon;
    public int GrowTime => _growTime;
    public Texture2D[] GrowStages => _growStages;
    public PackedScene Scene => _scene;
    public ItemData Loot => _loot;
}
