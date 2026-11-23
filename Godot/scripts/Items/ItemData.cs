using Godot;

/// <summary>
/// Classe de base de tous les objets du jeu (port d'ItemData.cs).
/// ScriptableObject Unity → Resource Godot : on crée les objets en .tres (clic droit → New Resource → ItemData),
/// et [GlobalClass] les fait apparaître dans cette liste.
/// </summary>
[GlobalClass]
public partial class ItemData : Resource
{
    [ExportGroup("Item Settings")]
    [Export] public Texture2D WorldSprite;
    [Export] public Texture2D Icon;
    [Export] public string Name = "";
    [Export(PropertyHint.MultilineText)] public string Description = "";
    [Export] public int Amount = 1;

    /// <summary>Taille maximale d'une pile de cet objet dans un stockage.</summary>
    [Export(PropertyHint.Range, "1,999")] private int _maxStack = 99;

    /// <summary>Taille maximale d'une pile. Surchargée par les outils, qui ne s'empilent pas.</summary>
    public virtual int MaxStack => _maxStack;

    /// <summary>
    /// Indique si l'objet est retiré de l'inventaire après une utilisation réussie.
    /// Faux par défaut (les outils ne se consomment pas) ; surchargé par les graines.
    /// </summary>
    public virtual bool IsConsumable => false;
}
