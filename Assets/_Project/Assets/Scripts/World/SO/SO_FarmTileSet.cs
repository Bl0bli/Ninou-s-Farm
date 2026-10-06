using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

/// <summary>
/// Une feuille de sol et ses versions labourée / labourée humide.
/// Les trois feuilles ont la même disposition : deux sprites à la même position (même rect)
/// se correspondent, la case labourée garde donc la forme du sol en dessous (« tuile miroir »).
/// </summary>
[Serializable]
public struct FarmSheet
{
    public Sprite[] Ground;
    public Sprite[] Hoed;
    public Sprite[] Watered;
}

/// <summary>
/// Catalogue des tuiles de ferme : associe (sprite du sol, état, arrosage) à la tuile à afficher.
/// Pour ajouter une feuille : sélectionner tous ses sprites dans le Project et les glisser sur le tableau.
/// </summary>
[CreateAssetMenu(fileName = "SO_FarmTileSet", menuName = "Scriptable Objects/SO_FarmTileSet")]
public class SO_FarmTileSet : ScriptableObject
{
    [SerializeField] private FarmSheet[] _sheets = Array.Empty<FarmSheet>();

    [Tooltip("Utilisées quand le sprite du sol n'a pas de correspondance (biome non couvert).")]
    [SerializeField] private TileBase _fallbackHoedTile;
    [SerializeField] private TileBase _fallbackHoedWateredTile;

    // Tiles créées au premier accès (instances en mémoire, aucun asset modifié).
    private Dictionary<Sprite, (TileBase hoed, TileBase watered)> _bySprite;

    /// <summary>
    /// Renvoie la tuile à afficher sur la couche de ferme, ou null s'il n'y a rien à afficher.
    /// </summary>
    public TileBase GetTile(Sprite groundSprite, TileState state, bool isWatered)
    {
        if (state == TileState.NONE) return null;

        _bySprite ??= BuildLookup();
        (TileBase hoed, TileBase watered) tiles = default;
        if (groundSprite != null) _bySprite.TryGetValue(groundSprite, out tiles);

        TileBase hoed = tiles.hoed != null ? tiles.hoed : _fallbackHoedTile;
        if (!isWatered) return hoed;

        TileBase watered = tiles.watered != null ? tiles.watered : _fallbackHoedWateredTile;
        return watered != null ? watered : hoed;
    }

    private Dictionary<Sprite, (TileBase, TileBase)> BuildLookup()
    {
        Dictionary<Sprite, (TileBase, TileBase)> lookup = new Dictionary<Sprite, (TileBase, TileBase)>();

        foreach (FarmSheet sheet in _sheets)
        {
            if (sheet.Ground == null) continue;
            Dictionary<Rect, Sprite> hoedByRect = IndexByRect(sheet.Hoed);
            Dictionary<Rect, Sprite> wateredByRect = IndexByRect(sheet.Watered);

            foreach (Sprite ground in sheet.Ground)
            {
                if (ground == null || !hoedByRect.TryGetValue(ground.rect, out Sprite hoed)) continue;
                wateredByRect.TryGetValue(ground.rect, out Sprite watered);
                lookup[ground] = (CreateTile(hoed), CreateTile(watered));
            }
        }
        return lookup;
    }

    private static Dictionary<Rect, Sprite> IndexByRect(Sprite[] sprites)
    {
        Dictionary<Rect, Sprite> byRect = new Dictionary<Rect, Sprite>();
        if (sprites == null) return byRect;
        foreach (Sprite sprite in sprites)
            if (sprite != null) byRect[sprite.rect] = sprite;
        return byRect;
    }

    private static Tile CreateTile(Sprite sprite)
    {
        if (sprite == null) return null;
        Tile tile = CreateInstance<Tile>();
        tile.sprite = sprite;
        tile.colliderType = Tile.ColliderType.None; // le labour ne bloque pas le joueur
        return tile;
    }

    // Reconstruit les correspondances si on modifie les feuilles dans l'inspecteur.
    private void OnValidate() => _bySprite = null;
}
