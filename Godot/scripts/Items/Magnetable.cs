using Godot;

/// <summary>
/// Marque un objet comme attirable par l'aimant du joueur (port de Magnetable.cs).
/// Composant Unity → nœud enfant : l'objet attiré est son parent (Node2D).
/// </summary>
public partial class Magnetable : Node
{
    /// <summary>L'objet à déplacer.</summary>
    public Node2D Target => GetParent<Node2D>();

    /// <summary>GetComponent&lt;Magnetable&gt; : cherche ce marqueur parmi les enfants directs d'un nœud.</summary>
    public static Magnetable FindIn(Node node)
    {
        foreach (Node child in node.GetChildren())
            if (child is Magnetable magnetable) return magnetable;
        return null;
    }
}
