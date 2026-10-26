using UnityEngine;

[CreateAssetMenu(fileName = "ItemData", menuName = "Items/ItemData")]
public class ItemData : ScriptableObject
{
    [Header("Item Settings")] 
    public Sprite WorldSprite;
    public Sprite Icon;
    public string Name;
    public string Description;
    public int Amount;

    [Tooltip("Taille maximale d'une pile de cet objet dans un stockage.")]
    [Min(1)] [SerializeField] private int _maxStack = 99;

    /// <summary>Taille maximale d'une pile. Surchargée par les outils, qui ne s'empilent pas.</summary>
    public virtual int MaxStack => _maxStack;

    /// <summary>
    /// Indique si l'objet est retiré de l'inventaire après une utilisation réussie.
    /// Faux par défaut (les outils ne se consomment pas) ; surchargé par les graines.
    /// </summary>
    public virtual bool IsConsumable => false;
}
