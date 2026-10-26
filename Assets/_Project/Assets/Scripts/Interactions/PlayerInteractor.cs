using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Interactions du joueur : cherche les objets interactifs qui chevauchent sa zone d'interaction
/// (le collider du joueur placé sur le layer « IntaractHitBox »), affiche l'indication de touche au-dessus du personnage et déclenche l'interaction
/// sur l'action « Interact ». Un objet qui sort de la portée reçoit EndInteraction (un coffre se referme).
/// Ajouté par PlayerController : rien à câbler dans le prefab.
/// </summary>
public class PlayerInteractor : MonoBehaviour
{
    private const string InteractActionName = "Interact";
    private const int MaxHits = 16;

    private readonly Collider2D[] _hits = new Collider2D[MaxHits];
    private readonly List<(IInteractable interactable, Transform transform)> _inRange =
        new List<(IInteractable, Transform)>();
    private readonly List<(IInteractable interactable, Transform transform)> _found =
        new List<(IInteractable, Transform)>();

    private PlayerController _player;
    private Rigidbody2D _ownBody;
    private InputAction _interactAction;
    private SpriteRenderer _prompt;
    private Vector2 _promptOffset;
    private Collider2D _hitbox;
    private ContactFilter2D _filter;

    /// <summary>Vrai si un objet est à portée.</summary>
    public bool HasTarget => _inRange.Count > 0;

    public void Init(PlayerController player, Sprite promptSprite, Vector2 promptOffset, string hitboxLayer)
    {
        _player = player;
        _promptOffset = promptOffset;
        _ownBody = GetComponentInParent<Rigidbody2D>();
        _hitbox = FindHitbox(hitboxLayer);
        _filter = new ContactFilter2D { useTriggers = true };
        _prompt = CreatePrompt(promptSprite != null ? promptSprite : CreatePlaceholderSprite());

        PlayerInput input = GetComponentInParent<PlayerInput>();
        if (input == null) input = transform.root.GetComponentInChildren<PlayerInput>();
        _interactAction = input != null ? input.actions.FindAction(InteractActionName) : null;

        if (_interactAction == null) Debug.LogWarning($"[PlayerInteractor] Action d'input « {InteractActionName} » introuvable.");
        else _interactAction.performed += OnInteract;
    }

    private void OnDestroy()
    {
        if (_interactAction != null) _interactAction.performed -= OnInteract;
        if (_prompt != null) Destroy(_prompt.gameObject);
    }

    private void FixedUpdate()
    {
        if (_hitbox == null || !_hitbox.enabled) return;
        Scan();
    }

    /// <summary>
    /// Le collider du joueur qui est sur le layer de la zone d'interaction.
    /// </summary>
    private Collider2D FindHitbox(string layerName)
    {
        int layer = LayerMask.NameToLayer(layerName);
        if (layer < 0)
        {
            Debug.LogWarning($"[PlayerInteractor] Le layer « {layerName} » n'existe pas : aucune interaction possible.");
            return null;
        }

        Transform root = _ownBody != null ? _ownBody.transform : transform.root;
        foreach (Collider2D collider in root.GetComponentsInChildren<Collider2D>(true))
            if (collider.gameObject.layer == layer) return collider;

        Debug.LogWarning($"[PlayerInteractor] Aucun collider du joueur sur le layer « {layerName} » : aucune interaction possible.");
        return null;
    }

    /// <summary>
    /// Met à jour la liste des objets à portée ; ceux qui en sortent reçoivent EndInteraction.
    /// </summary>
    private void Scan()
    {
        _found.Clear();
        int count = _hitbox.Overlap(_filter, _hits);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = _hits[i];
            if (_ownBody != null && hit.attachedRigidbody == _ownBody) continue; // les colliders du joueur lui-même

            IInteractable interactable = hit.GetComponentInParent<IInteractable>();
            if (interactable == null || _found.Exists(e => e.interactable == interactable)) continue;
            _found.Add((interactable, ((Component)interactable).transform));
        }

        foreach ((IInteractable interactable, Transform owner) in _inRange)
        {
            if (owner != null && !_found.Exists(e => e.interactable == interactable)) interactable.EndInteraction();
        }

        _inRange.Clear();
        _inRange.AddRange(_found);
    }

    private void OnInteract(InputAction.CallbackContext context)
    {
        IInteractable target = GetNearest();
        target?.Interact();
    }

    private void LateUpdate()
    {
        bool show = HasTarget && _player != null && _player.Visual != null;
        _prompt.enabled = show;
        if (!show) return;

        SpriteRenderer visual = _player.Visual;
        _prompt.transform.position = (Vector2)visual.transform.position + _promptOffset;
        _prompt.sortingLayerName = visual.sortingLayerName;
        _prompt.sortingOrder = visual.sortingOrder + 10;
    }

    /// <summary>
    /// Plusieurs objets à portée : on choisit le plus proche du centre de la zone d'interaction.
    /// </summary>
    private IInteractable GetNearest()
    {
        IInteractable nearest = null;
        float best = float.MaxValue;
        Vector2 feet = _hitbox != null ? (Vector2)_hitbox.bounds.center : (Vector2)transform.position;

        foreach ((IInteractable interactable, Transform owner) in _inRange)
        {
            if (owner == null) continue;
            float distance = ((Vector2)owner.position - feet).sqrMagnitude;
            if (distance >= best) continue;
            best = distance;
            nearest = interactable;
        }
        return nearest;
    }

    // Débogage : en Play Mode, la vue Scene montre la zone d'interaction (jaune) et les objets à portée (vert).
    private void OnDrawGizmos()
    {
        if (!Application.isPlaying || _player == null) return;

        if (_hitbox == null) return;

        Bounds bounds = _hitbox.bounds;
        Gizmos.color = HasTarget ? Color.green : Color.yellow;
        Gizmos.DrawWireCube(bounds.center, bounds.size);

        Gizmos.color = Color.green;
        foreach ((IInteractable _, Transform owner) in _inRange)
            if (owner != null) Gizmos.DrawLine(bounds.center, owner.position);
    }

    private static SpriteRenderer CreatePrompt(Sprite sprite)
    {
        GameObject go = new GameObject("InteractPrompt");
        SpriteRenderer renderer = go.AddComponent<SpriteRenderer>();
        renderer.sprite = sprite;
        renderer.enabled = false;
        return renderer;
    }

    // En attendant le sprite de la touche E : une touche de clavier dessinée en 12x12 px (16 PPU).
    private static Sprite CreatePlaceholderSprite()
    {
        const int size = 12;
        Color border = new Color(0.27f, 0.17f, 0.14f);
        Color fill = new Color(0.96f, 0.91f, 0.80f);
        Color shade = new Color(0.82f, 0.72f, 0.58f);

        Texture2D texture = new Texture2D(size, size) { filterMode = FilterMode.Point };
        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool corner = (x == 0 || x == size - 1) && (y == 0 || y == size - 1);
                bool edge = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                Color color = corner ? Color.clear : edge ? border : y == 1 ? shade : fill;
                texture.SetPixel(x, y, color);
            }
        }
        texture.Apply();
        return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), 16f);
    }
}
