using Godot;

/// <summary>
/// Heure du jeu en texte. Version minimale en attendant le port de TimeUI (ciel jour / coucher / nuit, flèche).
/// </summary>
public partial class ClockLabel : Label
{
    public override void _Process(double delta) => Text = $"{TimeManager.Hour:00}:{TimeManager.Minute:00}";
}
