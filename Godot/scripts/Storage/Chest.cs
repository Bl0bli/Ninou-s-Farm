using System;
using Godot;

/// <summary>
/// Coffre : un Storage posé dans le monde, qu'on ouvre et ferme avec la touche E (port de Chest.cs).
/// Animator (booléen Open) → AnimatedSprite2D avec deux animations, « open » et « close ».
/// </summary>
public partial class Chest : Storage, IInteractable
{
    [Export] private int _slotsAmount = 20;
    [Export] private ItemData[] _startingItems = Array.Empty<ItemData>();
    [Export] private AnimatedSprite2D _sprite;

    private bool _isOpen;

    /// <summary>Un coffre vient d'être ouvert : l'interface du coffre s'y relie.</summary>
    public static event Action<Chest> OnChestOpened;

    /// <summary>Un coffre vient d'être fermé.</summary>
    public static event Action<Chest> OnChestClosed;

    public bool IsOpen => _isOpen;

    public override void _Ready()
    {
        Initialize(Math.Max(_startingItems.Length, _slotsAmount));
        base._Ready();

        foreach (ItemData item in _startingItems)
        {
            if (item != null) Add(item, item.Amount);
        }

        // Fermé au départ : dernière image de l'animation de fermeture.
        if (_sprite != null)
        {
            _sprite.Animation = "close";
            _sprite.Frame = _sprite.SpriteFrames.GetFrameCount("close") - 1;
        }
    }

    public bool Interact()
    {
        if (_isOpen) Close();
        else Open();
        return true;
    }

    private void Open()
    {
        _isOpen = true;
        _sprite?.Play("open");
        OnChestOpened?.Invoke(this);
    }

    private void Close()
    {
        _isOpen = false;
        _sprite?.Play("close");
        OnChestClosed?.Invoke(this);
    }

    /// <summary>Le joueur s'éloigne : un coffre ouvert se referme.</summary>
    public void EndInteraction()
    {
        if (_isOpen) Close();
    }

    // Un coffre retiré de la scène pendant qu'il est ouvert ne doit pas laisser son interface affichée.
    public override void _ExitTree()
    {
        if (_isOpen) Close();
    }
}
