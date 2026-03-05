using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(fileName = "CommonItem", menuName = "Scriptable Objects/CommonItem")]
public class CommonItem : ScriptableObject
{ 
    [Range(0, 1)] public float SpawnRate = 1; 
    public TileBase ItemTile;
}
