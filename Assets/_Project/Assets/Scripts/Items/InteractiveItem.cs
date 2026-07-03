using UnityEngine;

[CreateAssetMenu(fileName = "InteractiveItem", menuName = "Scriptable Objects/InteractiveItem")]
public class InteractiveItem : ScriptableObject
{
    [Range(0, 1)] public float SpawnRate = 1;
    public Vector2Int FootPrint = new Vector2Int(1, 1);
    public GameObject Prefab;
}
