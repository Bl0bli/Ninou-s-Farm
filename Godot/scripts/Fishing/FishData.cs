using Godot;

/// <summary>
/// Un poisson pêchable : un objet d'inventaire qui porte aussi sa difficulté (port de FishData.cs).
/// Les valeurs sont des réglages partagés : l'état d'une pêche vit dans PlayerFishing, jamais ici.
/// </summary>
[GlobalClass]
public partial class FishData : ItemData
{
    [ExportGroup("Fishing Difficulty")]
    /// <summary>Largeur de la zone à viser, en proportion de la jauge (0,05 = très étroite, 0,4 = large).</summary>
    [Export(PropertyHint.Range, "0.05,0.5")] private float _zoneWidth = 0.25f;

    /// <summary>Vitesse du curseur, en longueurs de jauge par seconde.</summary>
    [Export(PropertyHint.Range, "0.1,10,or_greater")] private float _cursorSpeed = 1f;

    public float ZoneWidth => _zoneWidth;
    public float CursorSpeed => _cursorSpeed;
}
