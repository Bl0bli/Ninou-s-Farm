using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

[CreateAssetMenu(menuName = "Custom Tiles/Slope Connected Rule Tile")]
public class SlopeConnectedRuleTile : RuleTile
{
    [Header("Tuiles amies (qui connectent avec l'herbe)")]
    public List<TileBase> SlopesToConnect;
    
    /// <summary>
    /// Détermine si une tuile voisine correspond aux règles de connexion, incluant les pentes amies.
    /// </summary>
    /// <param name="neighbor">Le type de voisin testé.</param>
    /// <param name="tile">La tuile voisine à tester.</param>
    /// <returns>Retourne true si la tuile correspond à la règle, sinon false.</returns>
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
