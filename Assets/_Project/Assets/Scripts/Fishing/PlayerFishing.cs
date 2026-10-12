using UnityEngine;

/// <summary>
/// Orchestre une session de pêche : attente de la morsure, jauge à valider, résultat.
/// C'est lui qui pilote l'Animator (FISH, FishBite, FishCaught, IsFishing), verrouille le déplacement
/// et fait tomber le poisson. L'état de la session vit ici ; FishingRodData ne contient que des réglages.
///
/// Phases : Waiting (lancer + attente) → Biting (jauge) → Ending (animation de fin) → None.
/// </summary>
public class PlayerFishing : MonoBehaviour
{
    private enum Phase { None, Waiting, Biting, Ending }

    private const string FishingStateTag = "Fishing";
    private const float EndingTimeout = 8f;

    private static readonly int FishHash = Animator.StringToHash("FISH");
    private static readonly int FishBiteHash = Animator.StringToHash("FishBite");
    private static readonly int FishCaughtHash = Animator.StringToHash("FishCaught");
    private static readonly int IsFishingHash = Animator.StringToHash("IsFishing");

    private Animator _animator;
    private PlayerMovements _movements;
    private UIFishingGauge _ui;
    private int _layer;

    private Phase _phase = Phase.None;
    private FishingRodData _rod;
    private Vector2Int _waterCell;
    private FishData _fish;
    private FishingGauge _gauge;
    private float _timer;
    private int _endingFrames;

    /// <summary>Vrai tant qu'une pêche est en cours : le clic est alors dirigé vers Confirm().</summary>
    public bool IsActive => _phase != Phase.None;

    public void Init(Animator animator, PlayerMovements movements)
    {
        _animator = animator;
        _movements = movements;
        _layer = Mathf.Max(0, animator.GetLayerIndex("IDLE"));
        _ui = new GameObject("UIFishingGauge").AddComponent<UIFishingGauge>();
    }

    /// <summary>
    /// Démarre une pêche vers la case d'eau visée. Renvoie false si une pêche est déjà en cours.
    /// </summary>
    public bool TryBegin(FishingRodData rod, Vector2Int waterCell)
    {
        if (IsActive || _animator == null) return false;

        _rod = rod;
        _waterCell = waterCell;

        // Un trigger resté armé se déclencherait plus tard, au mauvais moment.
        _animator.ResetTrigger(FishBiteHash);
        _animator.ResetTrigger(FishCaughtHash);
        _animator.SetBool(IsFishingHash, true);
        _animator.SetTrigger(FishHash);

        _movements.CanMove = false;
        _timer = rod.RollBiteDelay();
        _phase = Phase.Waiting;
        return true;
    }

    /// <summary>
    /// Clic du joueur pendant la pêche. Pendant l'attente, il remonte la ligne (rien ne sort) ;
    /// pendant la morsure, il valide la jauge.
    /// </summary>
    public void Confirm()
    {
        switch (_phase)
        {
            case Phase.Waiting:
                Finish(false, showGauge: false);
                break;
            case Phase.Biting:
                Finish(_gauge.Confirm(), showGauge: true);
                break;
        }
    }

    private void Update()
    {
        switch (_phase)
        {
            case Phase.Waiting:
                _timer -= Time.deltaTime;
                if (_timer <= 0f) StartBite();
                break;

            case Phase.Biting:
                _gauge.Tick(Time.deltaTime);
                _timer -= Time.deltaTime;
                if (_timer <= 0f)
                {
                    _gauge.Stop();
                    Finish(false, showGauge: true);
                }
                break;

            case Phase.Ending:
                _timer -= Time.deltaTime;
                if (_endingFrames > 0) _endingFrames--;
                else if (!IsInFishingState() || _timer <= 0f) EndSession();
                break;
        }
    }

    private void StartBite()
    {
        _fish = _rod.LootTable != null ? _rod.LootTable.Roll() : null;
        if (_fish == null)
        {
            Debug.LogWarning("[PlayerFishing] Table de poissons vide : la pêche est annulée.");
            Finish(false, showGauge: false);
            return;
        }

        // Zone placée au hasard ; sa largeur et la vitesse du curseur dépendent du poisson tiré.
        float half = _fish.ZoneWidth / 2f;
        float center = Random.Range(half, 1f - half);
        _gauge = new FishingGauge(center, _fish.ZoneWidth, _fish.CursorSpeed);
        _ui.Bind(_gauge);

        _animator.SetTrigger(FishBiteHash);
        _timer = _rod.HitWindow;
        _phase = Phase.Biting;
    }

    private void Finish(bool success, bool showGauge)
    {
        _animator.SetBool(IsFishingHash, false);
        if (success)
        {
            _animator.SetTrigger(FishCaughtHash);
            DropFish();
        }

        if (showGauge) _ui.ShowResult(success);
        else _ui.Hide();

        // L'animation de fin joue avant de rendre le déplacement : on attend la sortie des états de pêche.
        _timer = EndingTimeout;
        _endingFrames = 3;
        _phase = Phase.Ending;
    }

    private void DropFish()
    {
        GameObject prefab = Resources.Load<GameObject>("Prefabs/ItemPickable");
        if (prefab == null) return;

        ITileGrid grid = TileGridLocator.Current;
        Vector2 position = grid != null ? grid.GridToWorld(_waterCell) : (Vector2)transform.position;

        ItemPickable pickable = Instantiate(prefab, position, Quaternion.identity).GetComponent<ItemPickable>();
        pickable.Init(_fish);
    }

    // L'état courant ou l'état en transition porte le tag « Fishing » (posé par FishingAnimatorBuilder).
    private bool IsInFishingState() =>
        _animator.GetCurrentAnimatorStateInfo(_layer).IsTag(FishingStateTag) ||
        (_animator.IsInTransition(_layer) && _animator.GetNextAnimatorStateInfo(_layer).IsTag(FishingStateTag));

    private void EndSession()
    {
        _movements.CanMove = true;
        _gauge = null;
        _fish = null;
        _phase = Phase.None;
    }

    private void OnDisable()
    {
        // Désactivé en pleine pêche : on ne laisse ni le joueur bloqué, ni la jauge affichée.
        if (!IsActive) return;
        if (_ui != null) _ui.Hide();
        if (_movements != null) _movements.CanMove = true;
        _phase = Phase.None;
    }
}
