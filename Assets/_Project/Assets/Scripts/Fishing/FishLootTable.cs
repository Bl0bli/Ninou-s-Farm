using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Table de tirage pondérée des poissons : plus le poids d'une entrée est grand, plus elle sort souvent.
/// </summary>
[CreateAssetMenu(fileName = "FishLootTable", menuName = "Scriptable Objects/FishLootTable")]
public class FishLootTable : ScriptableObject
{
    [Serializable]
    public struct Entry
    {
        public FishData Fish;
        [Min(0f)] public float Weight;
    }

    [SerializeField] private List<Entry> _entries = new List<Entry>();

    /// <summary>
    /// Tire un poisson au hasard selon les poids. Renvoie null si la table est vide.
    /// </summary>
    public FishData Roll()
    {
        float total = 0f;
        foreach (Entry entry in _entries)
            if (entry.Fish != null) total += entry.Weight;

        if (total <= 0f) return null;

        float pick = UnityEngine.Random.value * total;
        foreach (Entry entry in _entries)
        {
            if (entry.Fish == null || entry.Weight <= 0f) continue;
            pick -= entry.Weight;
            if (pick <= 0f) return entry.Fish;
        }

        return null;
    }
}
