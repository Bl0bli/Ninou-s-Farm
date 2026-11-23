using Godot;

/// <summary>
/// Classe de base des outils (port de ToolData.cs).
/// Unity envoyait un Trigger à l'Animator (« HOE », « WATER ») ; ici on donne le préfixe de l'animation Godot
/// (« hoe », « watering », « chop »), complété par la direction regardée : « hoe_left », « watering_up »...
/// </summary>
public abstract partial class ToolData : ItemData, IUsable
{
    [ExportGroup("Tool Settings")]
    [Export] public float StaminaCost;
    [Export] public float Cooldown;

    // Un outil ne s'empile pas : chaque outil occupe son propre slot.
    public override int MaxStack => 1;

    [ExportGroup("Animation Settings")]
    /// <summary>Préfixe de l'animation du joueur dans player_frames.tres (ex : « hoe », « watering »).</summary>
    [Export] protected string _animationName = "";

    public abstract bool Use(PlayerController player, Vector2 targetPosition);
}
