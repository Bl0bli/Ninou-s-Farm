using System.Collections.Generic;
using Godot;

/// <summary>
/// Dessine tout le décor du monde (feuilles, fleurs, cailloux, champignons) depuis un seul nœud, avec _Draw.
/// Remplace la Tilemap « Items » d'Unity : ses cases de 1/3 d'unité tombent sur des pixels non entiers en Godot,
/// et des milliers de Sprite2D coûteraient bien plus qu'une liste d'images dessinées d'un coup.
/// Les images ne s'animent pas (les tuiles animées du décor Unity n'ont qu'une image).
/// </summary>
public partial class DecorLayer : Node2D
{
    /// <summary>Une image : texture, région, décalage de son centre par rapport au pivot (px), échelle.</summary>
    public readonly record struct Frame(Texture2D Texture, Rect2 Region, Vector2 Offset, float Scale);

    private readonly List<(Vector2 Position, Frame Frame)> _items = new();

    public int Count => _items.Count;

    public void Add(Vector2 globalPosition, Frame[] frames, float speed) => _items.Add((ToLocal(globalPosition), frames[0]));

    public void Clear()
    {
        _items.Clear();
        QueueRedraw();
    }

    public override void _Draw()
    {
        foreach ((Vector2 pos, Frame f) in _items)
        {
            Vector2 size = f.Region.Size * f.Scale;
            DrawTextureRectRegion(f.Texture, new Rect2(pos + f.Offset - size / 2f, size), f.Region);
        }
    }
}
