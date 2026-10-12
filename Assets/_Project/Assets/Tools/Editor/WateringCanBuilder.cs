using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Met en place l'arrosoir : asset WateringCanData (icône, trigger, images d'éclaboussure) et état
/// WATERING_Tree dans l'Animator du joueur (Blend Tree 2D Dir_x / Dir_y, comme HOE_Tree).
/// Idempotent : relancer l'outil met à jour l'existant sans rien dupliquer.
/// </summary>
public static class WateringCanBuilder
{
    private const string ControllerPath = "Assets/_Project/Assets/Characters/Pneem/Pneem_AnimatorController.controller";
    private const string ClipFolder = "Assets/_Project/Assets/Characters/Pneem/Watering";
    private const string AssetPath = "Assets/_Project/Assets/World/ToolData/WateringCanData.asset";
    private const string ToolsSheetPath = "Assets/_Project/Assets/Packs/Sprout Lands - Sprites - premium pack/Sprout Lands - Sprites - premium pack/Objects/Items/tools and meterials.png";
    private const string SplashSheetPath = "Assets/_Project/Assets/Packs/Sprout Lands - Sprites - premium pack/Sprout Lands - Sprites - premium pack/Characters/water from wateringcan frames.png";

    private const string TriggerName = "WATER";
    private const string StateName = "WATERING_Tree";
    private const int SplashCell = 48;

    private static readonly (string dir, Vector2 pos)[] Directions =
    {
        ("Back", new Vector2(0, 1)),
        ("Front", new Vector2(0, -1)),
        ("Left", new Vector2(-1, 0)),
        ("Right", new Vector2(1, 0)),
    };

    [MenuItem("Tools/Ninou's Farm/Build Watering Can")]
    public static void Build()
    {
        BuildAnimator();
        BuildAsset();
        AssetDatabase.SaveAssets();
    }

    private static void BuildAnimator()
    {
        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) { Debug.LogError($"[WateringCanBuilder] Controller introuvable : {ControllerPath}"); return; }

        AnimatorStateMachine machine = controller.layers
            .Select(l => FindMachine(l.stateMachine, "IDLE_Tree"))
            .FirstOrDefault(m => m != null);
        if (machine == null) { Debug.LogError("[WateringCanBuilder] Machine d'états contenant IDLE_Tree introuvable."); return; }

        AnimatorState idleTree = FindState(machine, "IDLE_Tree");

        if (controller.parameters.All(p => p.name != TriggerName))
            controller.AddParameter(TriggerName, AnimatorControllerParameterType.Trigger);

        AnimatorState state = FindState(machine, StateName) ?? machine.AddState(StateName, new Vector3(600, -250, 0));
        state.writeDefaultValues = true;

        // Arbre : remplacé à chaque passage pour ne pas laisser d'objets orphelins dans l'asset.
        if (state.motion is BlendTree old)
        {
            state.motion = null;
            AssetDatabase.RemoveObjectFromAsset(old);
            Object.DestroyImmediate(old, true);
        }

        BlendTree tree = new BlendTree
        {
            name = "Watering",
            blendType = BlendTreeType.SimpleDirectional2D,
            blendParameter = "Dir_x",
            blendParameterY = "Dir_y",
        };
        AssetDatabase.AddObjectToAsset(tree, controller);
        foreach ((string dir, Vector2 pos) in Directions)
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>($"{ClipFolder}/Watering_{dir}.anim");
            if (clip == null) { Debug.LogError($"[WateringCanBuilder] Clip manquant : Watering_{dir}"); continue; }
            tree.AddChild(clip, pos);
        }
        state.motion = tree;

        // Le joueur ne se déplace pas pendant qu'il arrose (comme pour la hache).
        if (state.behaviours.All(b => !(b is LockMovementBehaviour)))
            state.AddStateMachineBehaviour<LockMovementBehaviour>();

        // Any State → arrosage sur le trigger, puis retour à IDLE_Tree en fin de clip.
        foreach (AnimatorStateTransition t in machine.anyStateTransitions.Where(t => t.destinationState == state).ToArray())
            machine.RemoveAnyStateTransition(t);
        AnimatorStateTransition enter = machine.AddAnyStateTransition(state);
        enter.hasExitTime = false;
        enter.hasFixedDuration = true;
        enter.duration = 0.1f;
        enter.canTransitionToSelf = true;
        enter.AddCondition(AnimatorConditionMode.If, 0f, TriggerName);

        foreach (AnimatorStateTransition t in state.transitions.ToArray()) state.RemoveTransition(t);
        AnimatorStateTransition exit = state.AddTransition(idleTree);
        exit.hasExitTime = true;
        exit.exitTime = 1f;
        exit.hasFixedDuration = true;
        exit.duration = 0.1f;

        EditorUtility.SetDirty(controller);
        Debug.Log("[WateringCanBuilder] Animator : état WATERING_Tree câblé (trigger WATER).");
    }

    private static void BuildAsset()
    {
        WateringCanData can = AssetDatabase.LoadAssetAtPath<WateringCanData>(AssetPath);
        if (can == null)
        {
            can = ScriptableObject.CreateInstance<WateringCanData>();
            AssetDatabase.CreateAsset(can, AssetPath);
        }

        Sprite icon = AssetDatabase.LoadAllAssetsAtPath(ToolsSheetPath).OfType<Sprite>()
            .FirstOrDefault(s => s.name == "tools and meterials_0");

        SerializedObject so = new SerializedObject(can);
        so.FindProperty("Name").stringValue = "Watering Can";
        so.FindProperty("Amount").intValue = 1;
        if (icon != null)
        {
            so.FindProperty("Icon").objectReferenceValue = icon;
            so.FindProperty("WorldSprite").objectReferenceValue = icon;
        }
        so.FindProperty("_animationTriggerName").stringValue = TriggerName;

        // Eau : une ligne de la feuille (grille 48x48) par direction, de haut en bas : face, gauche, droite.
        Sprite[] sheet = AssetDatabase.LoadAllAssetsAtPath(SplashSheetPath).OfType<Sprite>()
            .Where(s => Mathf.Approximately(s.rect.width, SplashCell) && Mathf.Approximately(s.rect.height, SplashCell))
            .ToArray();
        float[] rows = sheet.Select(s => s.rect.y).Distinct().OrderByDescending(y => y).ToArray();

        if (rows.Length < 3)
            Debug.LogWarning("[WateringCanBuilder] Feuille d'eau pas découpée en grille 48x48 (3 lignes attendues) : pas d'effet d'eau.");

        // Mesures faites sur les feuilles (pixels, y vers le bas) :
        // - ligne 0 de l'eau : premier filet en (18, 26), juste sous l'arrosoir de face (bec en 15-21, 26) ;
        // - ligne 2 de l'eau : filet en (30, 27), sous l'arrosoir de dos (28-32), caché derrière le corps ;
        // - de côté, le bec est en (44, 26) à droite et (3, 26) à gauche : on y décale la ligne 0.
        // L'arrosoir verse de l'image 1 à l'image 6 du clip (0,1 s à 0,7 s) : les 9 images d'eau tiennent dans ces 0,6 s.
        so.FindProperty("_splashFrameRate").floatValue = 15f;
        so.FindProperty("_splashDelay").floatValue = 0.1f;

        SetSplash(so, "_splashFront", RowFrames(sheet, rows, 0), new Vector2Int(0, 0), false);
        SetSplash(so, "_splashBack", RowFrames(sheet, rows, 2), new Vector2Int(0, 0), true);
        SetSplash(so, "_splashRight", RowFrames(sheet, rows, 0), new Vector2Int(26, 0), false);
        SetSplash(so, "_splashLeft", RowFrames(sheet, rows, 0), new Vector2Int(-15, 0), false);

        so.ApplyModifiedPropertiesWithoutUndo();
        EditorUtility.SetDirty(can);
        Debug.Log($"[WateringCanBuilder] Asset prêt : {AssetPath} ({rows.Length} lignes d'eau).");
    }

    private static Sprite[] RowFrames(Sprite[] sheet, float[] rows, int row) =>
        row < rows.Length
            ? sheet.Where(s => Mathf.Approximately(s.rect.y, rows[row])).OrderBy(s => s.rect.x).ToArray()
            : new Sprite[0];

    private static void SetSplash(SerializedObject so, string field, Sprite[] frames, Vector2Int pixelOffset, bool behind)
    {
        SerializedProperty set = so.FindProperty(field);
        SerializedProperty list = set.FindPropertyRelative("Frames");
        list.arraySize = frames.Length;
        for (int i = 0; i < frames.Length; i++) list.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        set.FindPropertyRelative("PixelOffset").vector2IntValue = pixelOffset;
        set.FindPropertyRelative("Behind").boolValue = behind;
    }

    private static AnimatorState FindState(AnimatorStateMachine machine, string name)
    {
        AnimatorStateMachine owner = FindMachine(machine, name);
        return owner?.states.First(s => s.state.name == name).state;
    }

    private static AnimatorStateMachine FindMachine(AnimatorStateMachine machine, string stateName)
    {
        if (machine.states.Any(s => s.state.name == stateName)) return machine;
        foreach (ChildAnimatorStateMachine sub in machine.stateMachines)
        {
            AnimatorStateMachine found = FindMachine(sub.stateMachine, stateName);
            if (found != null) return found;
        }
        return null;
    }
}
