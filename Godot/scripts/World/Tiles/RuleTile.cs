using System;
using System.Collections.Generic;
using System.Text.Json;
using Godot;

/// <summary>
/// Équivalent Godot des RuleTile Unity : une liste ordonnée de règles « voisin = moi / pas moi » ;
/// la première règle qui correspond donne la tuile à poser (au hasard parmi plusieurs si la règle est Random).
///
/// Pourquoi ne pas utiliser les Terrains de Godot ? Les règles des RuleTiles Unity sont reprises telles quelles
/// (resources/tiles/rule_tiles.json, produit par tools/convert_unity_assets.py) : même résultat visuel qu'avant,
/// et la génération procédurale décide déjà de chaque case en code.
/// </summary>
public class RuleTile
{
    public const string RulesPath = "res://resources/tiles/rule_tiles.json";

    private const int This = 1;
    private const int NotThis = 2;

    /// <summary>Une tuile du TileSet : source d'atlas, coordonnées dans l'atlas, alternative.</summary>
    public readonly record struct TileRef(int Source, Vector2I Atlas, int Alternative = 0);

    private sealed class Rule
    {
        public (Vector2I Offset, int Value)[] Neighbors;
        public TileRef[] Tiles;
        public bool Random;
    }

    private readonly List<Rule> _rules = new();
    private readonly HashSet<string> _connects = new();

    public string Name { get; }

    private RuleTile(string name) => Name = name;

    private static Dictionary<string, RuleTile> _cache;

    /// <summary>Tuile « blocker » : case de collision pleine, posée sur le calque COLLISION.</summary>
    public static TileRef Blocker { get; private set; }

    /// <summary>Versions labourée / arrosée d'une feuille de sol : ids de sources du TileSet (-1 si absente).</summary>
    public readonly record struct FarmSheet(int Hoed, int Watered);

    private static readonly Dictionary<int, FarmSheet> FarmSheets = new();

    /// <summary>Feuille de ferme associée à la source d'atlas d'une tuile de sol.</summary>
    public static bool TryGetFarmSheet(int groundSource, out FarmSheet sheet)
    {
        _cache ??= LoadAll();
        return FarmSheets.TryGetValue(groundSource, out sheet) && sheet.Hoed >= 0;
    }

    /// <summary>Charge (une seule fois) toutes les RuleTiles du fichier JSON et renvoie celle demandée.</summary>
    public static RuleTile Get(string name)
    {
        _cache ??= LoadAll();
        if (_cache.TryGetValue(name, out RuleTile tile)) return tile;
        throw new KeyNotFoundException($"[RuleTile] '{name}' absente de {RulesPath}.");
    }

    private static Dictionary<string, RuleTile> LoadAll()
    {
        var result = new Dictionary<string, RuleTile>();
        using JsonDocument doc = JsonDocument.Parse(FileAccess.GetFileAsString(RulesPath));

        foreach (JsonProperty entry in doc.RootElement.EnumerateObject())
        {
            if (entry.Name == "farm_sheets")
            {
                foreach (JsonElement s in entry.Value.EnumerateArray())
                {
                    int Get(string key) => s.TryGetProperty(key, out JsonElement v) ? v.GetInt32() : -1;
                    FarmSheets[Get("ground")] = new FarmSheet(Get("hoed"), Get("watered"));
                }
                continue;
            }

            if (entry.Name == "blocker")
            {
                JsonElement atlas = entry.Value.GetProperty("atlas");
                Blocker = new TileRef(entry.Value.GetProperty("source").GetInt32(),
                    new Vector2I(atlas[0].GetInt32(), atlas[1].GetInt32()),
                    entry.Value.GetProperty("alternative").GetInt32());
                continue;
            }

            var tile = new RuleTile(entry.Name);
            foreach (JsonElement r in entry.Value.GetProperty("rules").EnumerateArray())
            {
                var neighbors = new List<(Vector2I, int)>();
                foreach (JsonElement n in r.GetProperty("neighbors").EnumerateArray())
                    neighbors.Add((new Vector2I(n[0].GetInt32(), n[1].GetInt32()), n[2].GetInt32()));

                var tiles = new List<TileRef>();
                foreach (JsonElement t in r.GetProperty("tiles").EnumerateArray())
                    tiles.Add(new TileRef(t[0].GetInt32(), new Vector2I(t[1].GetInt32(), t[2].GetInt32())));

                tile._rules.Add(new Rule
                {
                    Neighbors = neighbors.ToArray(),
                    Tiles = tiles.ToArray(),
                    Random = r.GetProperty("random").GetBoolean()
                });
            }
            if (entry.Value.TryGetProperty("connects", out JsonElement connects))
                foreach (JsonElement c in connects.EnumerateArray()) tile._connects.Add(c.GetString());
            result[entry.Name] = tile;
        }
        return result;
    }

    /// <summary>
    /// Choisit la tuile d'une case. isThis(case) indique si la case voisine porte cette même RuleTile
    /// (comme « tile == this » dans RuleTile.RuleMatch côté Unity).
    /// </summary>
    public TileRef? Resolve(Vector2I cell, Func<Vector2I, bool> isThis)
    {
        foreach (Rule rule in _rules)
        {
            if (!Matches(rule, cell, isThis)) continue;
            if (!rule.Random || rule.Tiles.Length == 1) return rule.Tiles[0];
            return rule.Tiles[PositionHash(cell) % rule.Tiles.Length];
        }
        return null;
    }

    /// <summary>
    /// Une voisine portant la RuleTile <paramref name="other"/> compte-t-elle comme « moi » ?
    /// Oui si c'est la même, ou une tuile amie (SlopeConnectedRuleTile.SlopesToConnect : les pentes pour les falaises).
    /// </summary>
    public bool Matches(string other) => other == Name || (other != null && _connects.Contains(other));

    /// <summary>Pose la tuile résolue sur un calque (ou efface la case si aucune règle ne correspond).</summary>
    public void Paint(TileMapLayer layer, Vector2I cell, Func<Vector2I, bool> isThis)
    {
        if (Resolve(cell, isThis) is TileRef t) layer.SetCell(cell, t.Source, t.Atlas, t.Alternative);
        else layer.EraseCell(cell);
    }

    private static bool Matches(Rule rule, Vector2I cell, Func<Vector2I, bool> isThis)
    {
        foreach ((Vector2I offset, int value) in rule.Neighbors)
        {
            bool same = isThis(cell + offset);
            if (value == This && !same) return false;
            if (value == NotThis && same) return false;
        }
        return true;
    }

    /// <summary>Hasard déterministe : une même case donne toujours la même variante.</summary>
    private static int PositionHash(Vector2I cell)
    {
        unchecked
        {
            uint h = (uint)(cell.X * 73856093) ^ (uint)(cell.Y * 19349663);
            h ^= h >> 13;
            h *= 0x5bd1e995;
            h ^= h >> 15;
            return (int)(h & 0x7fffffff);
        }
    }
}
