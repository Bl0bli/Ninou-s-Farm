using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

/// <summary>
/// Construit la scène Test_Tools : une copie de Test_1 sans génération procédurale,
/// avec une petite carte peinte à la main pour développer et tester les outils
/// (hache, houe, arrosoir...) sans attendre la génération du monde.
/// La scène est entièrement régénérée à chaque exécution : ne pas y faire de modifications
/// qu'on veut garder, modifier plutôt ce script.
/// </summary>
public static class ToolsTestSceneBuilder
{
    private const string SourceScenePath = "Assets/_Project/Scenes/Test_1.unity";
    private const string TargetScenePath = "Assets/_Project/Scenes/Test_Tools.unity";

    private const string GroundTilePath = "Assets/_Project/Assets/World/RuleTile_Grass.asset";
    private const string FloorTilePath = "Assets/_Project/Assets/World/RuleTile_Grass_High.asset";
    private const string WaterTilePath = "Assets/_Project/Assets/World/RuleTile_Water.asset";
    private const string TreePrefabPath = "Assets/_Project/Assets/Prefabs/Trees/Tree.prefab";
    private const string HoeAssetPath = "Assets/_Project/Assets/Scripts/Tools/HoeData.asset";

    // Carte : un rectangle de terre bordé d'eau, avec un étang (eau) et un plateau (falaise).
    private const int MapWidth = 24;
    private const int MapHeight = 16;
    private const int WaterMargin = 6;
    private static readonly RectInt Land = new RectInt(1, 1, MapWidth - 2, MapHeight - 2);
    private static readonly RectInt Pond = new RectInt(17, 10, 3, 3);
    private static readonly RectInt Plateau = new RectInt(3, 10, 4, 3);
    private static readonly Vector2Int PlayerCell = new Vector2Int(8, 6);
    private static readonly Vector2Int[] TreeCells = { new Vector2Int(11, 4), new Vector2Int(14, 8), new Vector2Int(17, 4) };

    [MenuItem("Tools/Ninou's Farm/Build Tools Test Scene")]
    public static void BuildFromMenu()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScenePath) != null &&
            !EditorUtility.DisplayDialog("Scène de test des outils",
                $"{TargetScenePath} existe déjà et va être entièrement régénérée depuis Test_1. Continuer ?",
                "Régénérer", "Annuler"))
            return;

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

        Build();
    }

    /// <summary>
    /// Point d'entrée en ligne de commande (batch mode) : -executeMethod ToolsTestSceneBuilder.BuildFromCommandLine
    /// </summary>
    public static void BuildFromCommandLine()
    {
        try
        {
            Build();
            EditorApplication.Exit(0);
        }
        catch (Exception e)
        {
            Debug.LogException(e);
            EditorApplication.Exit(1);
        }
    }

    private static void Build()
    {
        TileBase groundTile = LoadRequired<TileBase>(GroundTilePath);
        TileBase floorTile = LoadRequired<TileBase>(FloorTilePath);
        TileBase waterTile = LoadRequired<TileBase>(WaterTilePath);
        GameObject treePrefab = LoadRequired<GameObject>(TreePrefabPath);

        AssetDatabase.DeleteAsset(TargetScenePath);
        if (!AssetDatabase.CopyAsset(SourceScenePath, TargetScenePath))
            throw new InvalidOperationException($"Impossible de copier {SourceScenePath} vers {TargetScenePath}.");

        Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);

        GridWorld gridWorld = FindRequired<GridWorld>();
        Tilemap water = GetField<Tilemap>(gridWorld, "_waterTilemap");
        Tilemap ground = GetField<Tilemap>(gridWorld, "_groundTilemap");
        Tilemap floor = GetField<Tilemap>(gridWorld, "_floorTilemap");
        Tilemap collision = GetField<Tilemap>(gridWorld, "_collisionTilemap");

        ApplyEndOfGenerationEffects(gridWorld);
        Transform treesParent = RemoveProceduralGeneration(gridWorld);

        PaintMap(water, ground, floor, collision, waterTile, groundTile, floorTile);

        // GridWorld désactive ce collider pendant la génération et le réactive à la fin :
        // la scène source est donc sauvegardée avec le collider éteint. Sans GridWorld, on le rallume ici.
        TilemapCollider2D collisionCollider = collision.GetComponent<TilemapCollider2D>();
        if (collisionCollider != null) collisionCollider.enabled = true;
        AddTestGrid(ground.gameObject, ground, floor);
        AddFarmLayer(ground);
        PlaceTrees(treePrefab, ground, treesParent);
        PlacePlayer(ground);
        GiveTestTools();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();

        Debug.Log($"[ToolsTestSceneBuilder] Scène générée : {TargetScenePath} ({MapWidth}x{MapHeight}, {TreeCells.Length} arbres).");
    }

    /// <summary>
    /// GridWorld désactive l'écran de chargement à la fin de la génération (_onWorldInit).
    /// Sans génération, on applique cet effet directement dans la scène.
    /// </summary>
    private static void ApplyEndOfGenerationEffects(GridWorld gridWorld)
    {
        UnityEvent onWorldInit = GetField<UnityEvent>(gridWorld, "_onWorldInit");
        for (int i = 0; i < onWorldInit.GetPersistentEventCount(); i++)
        {
            if (onWorldInit.GetPersistentTarget(i) is GameObject target &&
                onWorldInit.GetPersistentMethodName(i) == nameof(GameObject.SetActive))
                target.SetActive(false);
        }
    }

    /// <summary>
    /// Retire GridWorld et WorldItemsGeneration. Renvoie le parent des objets interactifs
    /// pour y ranger les arbres de test.
    /// </summary>
    private static Transform RemoveProceduralGeneration(GridWorld gridWorld)
    {
        Transform treesParent = null;
        WorldItemsGeneration itemsGeneration = Object.FindFirstObjectByType<WorldItemsGeneration>(FindObjectsInactive.Include);
        if (itemsGeneration != null)
        {
            treesParent = GetField<Transform>(itemsGeneration, "_interactiveItemsParent");
            Object.DestroyImmediate(itemsGeneration);
        }

        Object.DestroyImmediate(gridWorld);
        return treesParent;
    }

    private static void PaintMap(Tilemap water, Tilemap ground, Tilemap floor, Tilemap collision,
        TileBase waterTile, TileBase groundTile, TileBase floorTile)
    {
        water.ClearAllTiles();
        ground.ClearAllTiles();
        floor.ClearAllTiles();
        collision.ClearAllTiles();

        for (int x = -WaterMargin; x < MapWidth + WaterMargin; x++)
        {
            for (int y = -WaterMargin; y < MapHeight + WaterMargin; y++)
            {
                Vector2Int cell = new Vector2Int(x, y);
                Vector3Int pos = new Vector3Int(x, y, 0);

                // Comme dans GridWorld : l'eau est posée partout, le sol la recouvre.
                water.SetTile(pos, waterTile);

                bool isLand = Land.Contains(cell) && !Pond.Contains(cell);
                if (!isLand)
                {
                    collision.SetTile(pos, waterTile);
                    continue;
                }

                ground.SetTile(pos, groundTile);
                if (Plateau.Contains(cell)) floor.SetTile(pos, floorTile);
            }
        }
    }

    private static void AddTestGrid(GameObject host, Tilemap ground, Tilemap floor)
    {
        TestTileGrid testGrid = host.AddComponent<TestTileGrid>();
        SerializedObject so = new SerializedObject(testGrid);
        so.FindProperty("_groundTilemap").objectReferenceValue = ground;
        so.FindProperty("_floorTilemap").objectReferenceValue = floor;
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    /// <summary>
    /// Couche d'affichage des cases labourées / arrosées, rendue juste au-dessus du sol,
    /// pilotée par FarmTilemapView. Pas de collider : le labour ne bloque pas le joueur.
    /// </summary>
    private static void AddFarmLayer(Tilemap ground)
    {
        GameObject go = new GameObject("Tilemap_FARM", typeof(Tilemap), typeof(TilemapRenderer));
        go.transform.SetParent(ground.transform.parent, false);
        go.transform.SetSiblingIndex(ground.transform.GetSiblingIndex() + 1);

        TilemapRenderer groundRenderer = ground.GetComponent<TilemapRenderer>();
        TilemapRenderer farmRenderer = go.GetComponent<TilemapRenderer>();
        farmRenderer.sortingLayerID = groundRenderer.sortingLayerID;
        farmRenderer.sortingOrder = groundRenderer.sortingOrder + 1;

        FarmTilemapView view = go.AddComponent<FarmTilemapView>();
        SerializedObject so = new SerializedObject(view);
        so.FindProperty("_farmTilemap").objectReferenceValue = go.GetComponent<Tilemap>();
        so.FindProperty("_groundTilemap").objectReferenceValue = ground;
        so.FindProperty("_tileSet").objectReferenceValue = FindFirstAsset<SO_FarmTileSet>();
        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void PlaceTrees(GameObject treePrefab, Tilemap ground, Transform parent)
    {
        foreach (Vector2Int cell in TreeCells)
        {
            GameObject tree = (GameObject)PrefabUtility.InstantiatePrefab(treePrefab, ground.gameObject.scene);
            if (parent != null) tree.transform.SetParent(parent, false);
            tree.transform.position = ground.GetCellCenterWorld(new Vector3Int(cell.x, cell.y, 0));
        }
    }

    /// <summary>
    /// GameManager positionnait le joueur au spawn calculé par la génération.
    /// Ici on le pose directement sur une case connue de la carte de test.
    /// </summary>
    private static void PlacePlayer(Tilemap ground)
    {
        GameManager gameManager = FindRequired<GameManager>();
        Transform player = GetField<Transform>(gameManager, "_playerTransform");
        if (player == null)
            throw new InvalidOperationException("GameManager._playerTransform n'est pas assigné dans Test_1.");

        player.position = ground.GetCellCenterWorld(new Vector3Int(PlayerCell.x, PlayerCell.y, 0));
    }

    /// <summary>
    /// Ajoute la hache et la houe aux objets de départ du joueur (dans cette scène uniquement).
    /// </summary>
    private static void GiveTestTools()
    {
        PlayerInventory inventory = FindRequired<PlayerInventory>();
        List<ItemData> tools = new List<ItemData>();

        AxeData axe = FindFirstAsset<AxeData>();
        if (axe != null) tools.Add(axe);
        tools.Add(GetOrCreateHoeAsset());

        SerializedObject so = new SerializedObject(inventory);
        SerializedProperty startingItems = so.FindProperty("_startingItems");

        HashSet<Object> existing = new HashSet<Object>();
        for (int i = 0; i < startingItems.arraySize; i++)
            existing.Add(startingItems.GetArrayElementAtIndex(i).objectReferenceValue);

        foreach (ItemData tool in tools.Where(t => !existing.Contains(t)))
        {
            startingItems.arraySize++;
            startingItems.GetArrayElementAtIndex(startingItems.arraySize - 1).objectReferenceValue = tool;
        }

        so.ApplyModifiedPropertiesWithoutUndo();
    }

    private static HoeData GetOrCreateHoeAsset()
    {
        HoeData hoe = FindFirstAsset<HoeData>();
        if (hoe != null) return hoe;

        hoe = ScriptableObject.CreateInstance<HoeData>();
        hoe.Name = "Hoe";
        hoe.Amount = 1;
        AssetDatabase.CreateAsset(hoe, HoeAssetPath);
        Debug.Log($"[ToolsTestSceneBuilder] Asset de houe créé : {HoeAssetPath} (pense à lui assigner une icône).");
        return hoe;
    }

    private static T FindFirstAsset<T>() where T : Object
    {
        string guid = AssetDatabase.FindAssets($"t:{typeof(T).Name}").FirstOrDefault();
        return guid == null ? null : AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
    }

    private static T LoadRequired<T>(string path) where T : Object
    {
        T asset = AssetDatabase.LoadAssetAtPath<T>(path);
        if (asset == null) throw new InvalidOperationException($"Asset introuvable : {path}");
        return asset;
    }

    private static T FindRequired<T>() where T : Object
    {
        T found = Object.FindFirstObjectByType<T>(FindObjectsInactive.Include);
        if (found == null) throw new InvalidOperationException($"Aucun {typeof(T).Name} trouvé dans {SourceScenePath}.");
        return found;
    }

    private static T GetField<T>(object target, string fieldName)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        if (field == null) throw new MissingFieldException(target.GetType().Name, fieldName);
        return (T)field.GetValue(target);
    }
}
