using UnityEngine;
public abstract class ToolData : ItemData
{
    [Header("Tool Settings")]
    public float StaminaCost;
    public float Cooldown;

    public abstract bool Use();
}
