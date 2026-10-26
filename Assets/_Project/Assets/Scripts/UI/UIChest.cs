using UnityEngine;

/// <summary>
/// Interface du coffre : la même grille que l'inventaire (UIStorage), sans toolbar.
/// Un seul objet sert tous les coffres. Il vient d'un prefab (Resources/Prefabs/UI/UIChest) :
/// à la première ouverture d'un coffre, il est instancié s'il n'est pas déjà dans la scène,
/// puis réutilisé et relié au coffre ouvert à chaque fois.
/// Structure attendue du prefab : la racine (toujours active) porte UIChest et un Canvas, _panel est l'enfant affiché.
/// </summary>
public class UIChest : UIStorage
{
    private const string PrefabPath = "Prefabs/UI/UIChest";

    [Header("Chest")]
    [Tooltip("Panneau du coffre (copie du panneau d'inventaire, sans la toolbar). Masqué tant qu'aucun coffre n'est ouvert.")]
    [SerializeField] private GameObject _panel;

    private static UIChest _instance;
    private Chest _openChest;

    // Les événements des coffres sont statiques : on s'y abonne une seule fois, au lancement du jeu,
    // pour pouvoir créer l'interface au moment où le premier coffre s'ouvre.
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Install()
    {
        _instance = null;
        Chest.OnChestOpened -= HandleChestOpened;
        Chest.OnChestClosed -= HandleChestClosed;
        Chest.OnChestOpened += HandleChestOpened;
        Chest.OnChestClosed += HandleChestClosed;
    }

    private static void HandleChestOpened(Chest chest)
    {
        if (_instance == null) CreateFromPrefab();
        if (_instance != null) _instance.Show(chest);
    }

    private static void HandleChestClosed(Chest chest)
    {
        if (_instance != null) _instance.Hide(chest);
    }

    private static void CreateFromPrefab()
    {
        UIChest prefab = Resources.Load<UIChest>(PrefabPath);
        if (prefab == null)
        {
            Debug.LogError($"[UIChest] Prefab introuvable : Resources/{PrefabPath}. Aucune interface de coffre ne peut s'afficher.");
            return;
        }

        Instantiate(prefab).name = prefab.name; // Awake enregistre l'instance
    }

    private void Awake()
    {
        // Une seule interface de coffre : une copie posée en trop dans la scène est retirée.
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        if (_panel == null) return;

        CenterPanel();
        _panel.SetActive(false);
    }

    /// <summary>
    /// Contrairement à l'inventaire, le coffre s'affiche au milieu de l'écran, quelle que soit la mise en page du prefab.
    /// </summary>
    private void CenterPanel()
    {
        if (!(_panel.transform is RectTransform rect)) return;

        // Taille actuelle conservée : un panneau étiré sur l'écran garderait sinon une taille négative.
        Vector2 size = rect.rect.size;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
        rect.anchoredPosition = Vector2.zero;
    }

    private void OnDestroy()
    {
        if (_instance == this) _instance = null;
    }

    private void Show(Chest chest)
    {
        _openChest = chest;
        if (_panel != null) _panel.SetActive(true);
        Bind(chest);
    }

    private void Hide(Chest chest)
    {
        // Seul le coffre affiché peut fermer l'interface.
        if (chest != _openChest) return;

        _openChest = null;
        Bind(null);
        if (_panel != null) _panel.SetActive(false);
    }
}
