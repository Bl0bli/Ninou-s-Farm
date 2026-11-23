using Godot;

/// <summary>
/// Indication de touche. Sans image, dessine une touche de clavier 12x12 (comme le sprite créé en code dans Unity) :
/// en Godot, _Draw remplace la Texture2D générée pixel par pixel.
/// </summary>
public partial class InteractPrompt : Node2D
{
    public Texture2D Texture;

    public override void _Draw()
    {
        if (Texture != null)
        {
            DrawTexture(Texture, -Texture.GetSize() / 2f);
            return;
        }

        const int size = 12;
        var border = new Color(0.27f, 0.17f, 0.14f);
        var fill = new Color(0.96f, 0.91f, 0.80f);
        var shade = new Color(0.82f, 0.72f, 0.58f);
        var origin = new Vector2(-size / 2f, -size / 2f);

        DrawRect(new Rect2(origin + new Vector2(1, 0), new Vector2(size - 2, size)), border);
        DrawRect(new Rect2(origin + new Vector2(0, 1), new Vector2(size, size - 2)), border);
        DrawRect(new Rect2(origin + new Vector2(1, 1), new Vector2(size - 2, size - 2)), fill);
        DrawRect(new Rect2(origin + new Vector2(1, size - 2), new Vector2(size - 2, 1)), shade);
    }
}
