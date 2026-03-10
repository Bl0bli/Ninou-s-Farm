using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Custom Tiles/Slope Connected Rule Tile")]
public class SlopeConnectedRuleTile : RuleTile
{
    [Header("Tuiles amies (qui connectent avec l'herbe)")]
    public List<TileBase> SlopesToConnect;
    
    public override bool RuleMatch(int neighbor, TileBase tile)
    {
        bool isSlope = SlopesToConnect != null && SlopesToConnect.Contains(tile);

        switch (neighbor)
        {
            case TilingRuleOutput.Neighbor.This:
                return tile == this || isSlope;
            
            case TilingRuleOutput.Neighbor.NotThis:
                return tile != this && !isSlope;
        }

        return base.RuleMatch(neighbor, tile);
    }
}
