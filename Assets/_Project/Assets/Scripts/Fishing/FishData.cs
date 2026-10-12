using UnityEngine;

/// <summary>
/// Un poisson pêchable : un objet d'inventaire (nom, icône, sprite au sol) qui porte aussi sa difficulté.
/// Les valeurs sont des réglages partagés : l'état d'une pêche vit dans PlayerFishing, jamais ici.
/// </summary>
[CreateAssetMenu(fileName = "FishData", menuName = "Items/FishData")]
public class FishData : ItemData
{
    [Header("Fishing Difficulty")]
    [Tooltip("Largeur de la zone à viser, en proportion de la jauge (0.05 = très étroite, 0.4 = large).")]
    [Range(0.05f, 0.5f)] [SerializeField] private float _zoneWidth = 0.25f;

    [Tooltip("Vitesse du curseur, en longueurs de jauge par seconde.")]
    [Min(0.1f)] [SerializeField] private float _cursorSpeed = 1f;

    public float ZoneWidth => _zoneWidth;
    public float CursorSpeed => _cursorSpeed;
}
