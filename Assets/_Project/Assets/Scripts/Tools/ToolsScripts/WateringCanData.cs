using System;
using UnityEngine;

[CreateAssetMenu(fileName = "WateringCanData", menuName = "Items/Tools/WateringCanData")]
public class WateringCanData : ToolData
{
    /// <summary>
    /// L'eau d'une direction : ses images (cadre 48x48, comme le personnage) et le décalage, en pixels,
    /// qui amène le haut du filet d'eau sous le bec de l'arrosoir de l'animation.
    /// </summary>
    [Serializable]
    public struct SplashSet
    {
        public Sprite[] Frames;
        [Tooltip("Décalage en pixels de la feuille (x vers la droite, y vers le bas) depuis le sprite du personnage.")]
        public Vector2Int PixelOffset;
        [Tooltip("Dessiner l'eau derrière le personnage (arrosoir caché par le corps, de dos).")]
        public bool Behind;
    }

    [Header("Splash Effect")]
    [SerializeField] private SplashSet _splashFront;
    [SerializeField] private SplashSet _splashBack;
    [SerializeField] private SplashSet _splashLeft;
    [SerializeField] private SplashSet _splashRight;

    [Tooltip("Images par seconde de l'eau. 15 : les 9 images tiennent dans les 0,6 s où l'arrosoir verse.")]
    [SerializeField] private float _splashFrameRate = 15f;
    [Tooltip("Attente avant la première goutte : l'arrosoir se lève pendant la première image.")]
    [SerializeField] private float _splashDelay = 0.1f;
    [SerializeField] private float _pixelsPerUnit = 16f;

    /// <summary>
    /// Arrose la case visée : seule une case labourée peut être arrosée.
    /// </summary>
    /// <param name="player">Le contrôleur du joueur utilisant l'arrosoir.</param>
    /// <param name="targetPosition">La position visée sur la grille.</param>
    /// <returns>Retourne true si une case a été arrosée, sinon false.</returns>
    public override bool Use(PlayerController player, Vector2 targetPosition)
    {
        ITileGrid grid = TileGridLocator.Current;
        if (grid == null) return false;

        Vector2Int cell = grid.WorldToGrid(targetPosition);
        if (grid.GetTileAt(cell).State != TileState.HOED) return false;

        if (!string.IsNullOrEmpty(_animationTriggerName))
            player.TriggerAnimation(_animationTriggerName);

        PlaySplash(player);
        return grid.SetTileWatered(cell, true);
    }

    /// <summary>
    /// L'eau est calée sur le sprite du personnage puis décalée jusqu'au bec de l'arrosoir de la direction regardée.
    /// </summary>
    private void PlaySplash(PlayerController player)
    {
        SpriteRenderer visual = player.Visual;
        if (visual == null) return;

        SplashSet set = GetSplash(player.FacingDirection);
        Vector2 offset = new Vector2(set.PixelOffset.x, -set.PixelOffset.y) / _pixelsPerUnit;

        SpriteOneShot.Spawn(set.Frames, (Vector2)visual.transform.position + offset, _splashFrameRate, _splashDelay,
            visual.sortingLayerName, visual.sortingOrder + (set.Behind ? -1 : 1));
    }

    private SplashSet GetSplash(Vector2 facing)
    {
        if (Mathf.Abs(facing.x) > Mathf.Abs(facing.y))
            return facing.x > 0f ? _splashRight : _splashLeft;
        return facing.y > 0f ? _splashBack : _splashFront;
    }
}
