using UnityEngine;

[CreateAssetMenu(fileName = "AxeData", menuName = "Items/Tools/AxeData")]
public class AxeData : ToolData
{
    [Header("Specific Settings")] 
    public float Damage;
    
    public override bool Use()
    {
        Debug.Log("Axe used");
        return true;
    }
}
