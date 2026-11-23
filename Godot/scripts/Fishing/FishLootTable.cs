using Godot;

/// <summary>
/// Table de tirage pondérée des poissons : plus le poids d'une entrée est grand, plus elle sort souvent
/// (port de FishLootTable.cs).
/// </summary>
[GlobalClass]
public partial class FishLootTable : Resource
{
    // Array<Resource> plutôt que FishLootEntry[] : le format .tres d'un tableau typé sur une classe C#
    // dépend du chemin du script ; un tableau de Resource reste simple à écrire et à lire.
    [Export] private Godot.Collections.Array<Resource> _entries = new();

    /// <summary>Tire un poisson au hasard selon les poids. Renvoie null si la table est vide.</summary>
    public FishData Roll()
    {
        float total = 0f;
        foreach (Resource r in _entries)
            if (r is FishLootEntry { Fish: not null } e) total += e.Weight;

        if (total <= 0f) return null;

        float pick = GD.Randf() * total;
        foreach (Resource r in _entries)
        {
            if (r is not FishLootEntry { Fish: not null } e || e.Weight <= 0f) continue;
            pick -= e.Weight;
            if (pick <= 0f) return e.Fish;
        }
        return null;
    }
}
