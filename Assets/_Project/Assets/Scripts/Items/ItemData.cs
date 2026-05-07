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
}
