using Godot;

/// <summary>Arrosoir : arrose une case labourée et joue l'eau qui coule (port de WateringCanData.cs).</summary>
[GlobalClass]
public partial class WateringCanData : ToolData
{
    [ExportGroup("Splash Effect")]
    [Export] private SplashSet _splashFront;
    [Export] private SplashSet _splashBack;
    [Export] private SplashSet _splashLeft;
    [Export] private SplashSet _splashRight;

    /// <summary>Images par seconde de l'eau. 15 : les 9 images tiennent dans les 0,6 s où l'arrosoir verse.</summary>
    [Export] private float _splashFrameRate = 15f;

    /// <summary>Attente avant la première goutte : l'arrosoir se lève pendant la première image.</summary>
    [Export] private float _splashDelay = 0.1f;

    public override bool Use(PlayerController player, Vector2 targetPosition)
    {
        ITileGrid grid = TileGridLocator.Current;
        if (grid == null) return false;

        Vector2I cell = grid.WorldToGrid(targetPosition);
        if (grid.GetTileAt(cell).State != TileState.HOED) return false;

        if (!string.IsNullOrEmpty(_animationName))
            player.TriggerAnimation(_animationName);

        PlaySplash(player);
        return grid.SetTileWatered(cell, true);
    }

    /// <summary>
    /// L'eau est un enfant du sprite du personnage, décalée jusqu'au bec de l'arrosoir.
    /// Dans Unity le décalage en pixels était retourné (y vers le haut) ; Godot a déjà y vers le bas.
    /// </summary>
    private void PlaySplash(PlayerController player)
    {
        Node2D visual = player.Visual;
        SplashSet set = GetSplash(player.FacingDirection);
        if (visual == null || set == null) return;

        SpriteOneShot.Spawn(set.Frames, visual, set.PixelOffset, _splashFrameRate, _splashDelay, set.Behind);
    }

    private SplashSet GetSplash(Vector2 facing)
    {
        if (Mathf.Abs(facing.X) > Mathf.Abs(facing.Y))
            return facing.X > 0f ? _splashRight : _splashLeft;
        return facing.Y < 0f ? _splashBack : _splashFront; // Y vers le bas : « haut » = dos
    }
}
