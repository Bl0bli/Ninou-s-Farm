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

    /// <summary>
    /// Indique si l'objet est retiré de l'inventaire après une utilisation réussie.
    /// Faux par défaut (les outils ne se consomment pas) ; surchargé par les graines.
    /// </summary>
    public virtual bool IsConsumable => false;
}
