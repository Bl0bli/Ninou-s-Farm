using Godot;

/// <summary>
/// Joue une suite d'images une seule fois, puis se détruit (port de SpriteOneShot.cs).
/// Coroutine Unity (yield WaitForSeconds) → méthode async qui attend un timer Godot (await ToSignal).
/// </summary>
public partial class SpriteOneShot : Sprite2D
{
    /// <summary>Crée l'effet comme enfant de <paramref name="parent"/> et le lance.</summary>
    /// <param name="offset">Position locale par rapport au parent, en pixels.</param>
    /// <param name="behind">Dessiner derrière le parent (équivalent d'un sortingOrder inférieur).</param>
    public static void Spawn(Texture2D[] frames, Node2D parent, Vector2 offset, float frameRate, float delay, bool behind)
    {
        if (frames == null || frames.Length == 0 || frameRate <= 0f || parent == null) return;

        var effect = new SpriteOneShot
        {
            Name = "SpriteOneShot",
            Position = offset,
            ShowBehindParent = behind,
            Visible = false,
        };
        parent.AddChild(effect);
        effect.Play(frames, frameRate, delay);
    }

    private async void Play(Texture2D[] frames, float frameRate, float delay)
    {
        SceneTree tree = GetTree();
        if (delay > 0f) await ToSignal(tree.CreateTimer(delay), SceneTreeTimer.SignalName.Timeout);

        Visible = true;
        foreach (Texture2D frame in frames)
        {
            if (!IsInstanceValid(this)) return;
            Texture = frame;
            await ToSignal(tree.CreateTimer(1f / frameRate), SceneTreeTimer.SignalName.Timeout);
        }

        if (IsInstanceValid(this)) QueueFree();
    }
}
