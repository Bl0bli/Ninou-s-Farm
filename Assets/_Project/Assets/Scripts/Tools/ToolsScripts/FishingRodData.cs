using UnityEngine;

[CreateAssetMenu(fileName = "FishingRoadData", menuName = "Items/Tools/FishingRodData")]

public class FishingRodData : ToolData
{
    [Header("Fishing Settings")]
    [SerializeField] private FishLootTable _lootTable;

    [Tooltip("Délai avant la morsure (min, max), en secondes. Le minimum doit couvrir l'animation de lancer (~1 s).")]
    [SerializeField] private Vector2 _biteDelay = new Vector2(2f, 5f);

    [Tooltip("Temps laissé au joueur pour valider la jauge une fois que le poisson mord, en secondes.")]
    [Min(0.5f)] [SerializeField] private float _hitWindow = 6f;

    public FishLootTable LootTable => _lootTable;
    public float HitWindow => _hitWindow;

    public float RollBiteDelay() => Random.Range(Mathf.Min(_biteDelay.x, _biteDelay.y), Mathf.Max(_biteDelay.x, _biteDelay.y));

    /// <summary>
    /// Lance la canne vers la case visée : la pêche démarre si c'est de l'eau.
    /// </summary>
    /// <param name="player">Le contrôleur du joueur utilisant la canne à pêche.</param>
    /// <param name="targetPosition">La position visée sur la grille.</param>
    /// <returns>Retourne true si une pêche a démarré, sinon false (case de sol, pêche déjà en cours).</returns>
    public override bool Use(PlayerController player, Vector2 targetPosition)
    {
        ITileGrid grid = TileGridLocator.Current;
        if (grid == null) return false;

        Vector2Int cell = grid.WorldToGrid(targetPosition);
        if (grid.GetTileAt(cell).Type != TileType.WATER) return false;

        return player.Fishing.TryBegin(this, cell);
    }
}
