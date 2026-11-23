using System.Collections.Generic;
using Godot;

/// <summary>
/// Interactions du joueur (port de PlayerInteractor.cs) : cherche les objets interactifs dont l'Area2D chevauche
/// cette zone, affiche l'indication de touche au-dessus du personnage, et déclenche l'interaction sur « interact » (E).
/// Un objet qui sort de la portée reçoit EndInteraction (un coffre se referme).
///
/// Unity : Collider2D.Overlap sur un layer dédié. Godot : une Area2D dont le masque ne voit que le calque
/// « interactables » ; GetOverlappingAreas() donne directement la liste.
/// </summary>
public partial class PlayerInteractor : Area2D
{
    [Export] private PlayerController _player;
    /// <summary>Image affichée au-dessus du personnage. Vide : une touche de clavier dessinée par défaut.</summary>
    [Export] private Texture2D _promptTexture;
    /// <summary>Décalage de l'indication par rapport au sprite du personnage, en pixels.</summary>
    [Export] private Vector2 _promptOffset = new(0, -18);

    private readonly List<(IInteractable interactable, Node2D owner)> _inRange = new();
    private readonly List<(IInteractable interactable, Node2D owner)> _found = new();
    private InteractPrompt _prompt;

    /// <summary>Vrai si un objet est à portée.</summary>
    public bool HasTarget => _inRange.Count > 0;

    public override void _Ready()
    {
        _prompt = new InteractPrompt { Texture = _promptTexture, Visible = false, ZIndex = 100 };
        _player.Visual.AddChild(_prompt);
        _prompt.Position = _promptOffset;
    }

    public override void _PhysicsProcess(double delta)
    {
        Scan();
        _prompt.Visible = HasTarget;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (!e.IsActionPressed("interact")) return;
        GetNearest()?.Interact();
        GetViewport().SetInputAsHandled();
    }

    /// <summary>Met à jour la liste des objets à portée ; ceux qui en sortent reçoivent EndInteraction.</summary>
    private void Scan()
    {
        _found.Clear();
        foreach (Area2D area in GetOverlappingAreas())
        {
            (IInteractable interactable, Node2D owner) = FindInteractable(area);
            if (interactable == null || _found.Exists(e => e.interactable == interactable)) continue;
            _found.Add((interactable, owner));
        }

        foreach ((IInteractable interactable, Node2D owner) in _inRange)
        {
            if (IsInstanceValid(owner) && !_found.Exists(e => e.interactable == interactable)) interactable.EndInteraction();
        }

        _inRange.Clear();
        _inRange.AddRange(_found);
    }

    /// <summary>GetComponentInParent&lt;IInteractable&gt; : l'Area2D elle-même ou l'un de ses parents.</summary>
    private static (IInteractable, Node2D) FindInteractable(Node node)
    {
        for (Node n = node; n != null; n = n.GetParent())
            if (n is IInteractable interactable) return (interactable, n as Node2D ?? node as Node2D);
        return (null, null);
    }

    /// <summary>Plusieurs objets à portée : on choisit le plus proche des pieds du joueur.</summary>
    private IInteractable GetNearest()
    {
        IInteractable nearest = null;
        float best = float.MaxValue;
        Vector2 feet = _player.FeetPosition;

        foreach ((IInteractable interactable, Node2D owner) in _inRange)
        {
            if (!IsInstanceValid(owner)) continue;
            float distance = owner.GlobalPosition.DistanceSquaredTo(feet);
            if (distance >= best) continue;
            best = distance;
            nearest = interactable;
        }
        return nearest;
    }
}
