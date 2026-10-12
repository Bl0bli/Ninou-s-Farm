using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

/// <summary>
/// Câble la pêche dans l'Animator du joueur : états START / IDLE / STRUGGLE / QUIT / SHOW,
/// chacun étant un Blend Tree 2D piloté par Dir_x / Dir_y (comme HOE_Tree), et leurs transitions.
/// Idempotent : relancer l'outil reconstruit les états de pêche sans toucher aux autres.
///
/// Flux : FISH (trigger) → START → IDLE (boucle)
///   IDLE → STRUGGLE         sur FishBite (trigger)
///   IDLE / STRUGGLE → SHOW  sur FishCaught (trigger)
///   IDLE / STRUGGLE → QUIT  quand IsFishing passe à false
///   QUIT / SHOW → IDLE_Tree (fin du clip)
/// </summary>
public static class FishingAnimatorBuilder
{
    private const string ControllerPath = "Assets/_Project/Assets/Characters/Pneem/Pneem_AnimatorController.controller";
    private const string FishingFolder = "Assets/_Project/Assets/Characters/Pneem/Fishing";
    private const string ShowFolder = "Assets/_Project/Assets/Characters/Pneem/Show";

    // Mêmes positions que HOE_Tree : Back = haut, Front = bas.
    private static readonly (string dir, Vector2 pos)[] Directions =
    {
        ("Back", new Vector2(0, 1)),
        ("Front", new Vector2(0, -1)),
        ("Left", new Vector2(-1, 0)),
        ("Right", new Vector2(1, 0)),
    };

    private static readonly string[] OldStates =
    {
        "FISHING_STRUGGLE_FRONT", "FISHING_IDLE_FRONT", "FISHING_QUIT_FRONT", "SHOW_FRONT",
    };

    [MenuItem("Tools/Ninou's Farm/Fishing/Build Fishing Animator")]
    public static void Build()
    {
        // Les clips Right sont indispensables : on les (re)crée depuis Left avant de câbler.
        FishingClipTools.BuildRightClips();

        AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerPath);
        if (controller == null) { Debug.LogError($"[FishingAnimatorBuilder] Controller introuvable : {ControllerPath}"); return; }

        AnimatorStateMachine machine = FindMachineWithState(controller, "IDLE_Tree");
        if (machine == null) { Debug.LogError("[FishingAnimatorBuilder] Machine d'états contenant IDLE_Tree introuvable."); return; }
        AnimatorState idleTree = machine.states.First(s => s.state.name == "IDLE_Tree").state;

        EnsureParameter(controller, "IsFishing", AnimatorControllerParameterType.Bool);
        EnsureParameter(controller, "FishCaught", AnimatorControllerParameterType.Trigger);
        EnsureParameter(controller, "FishBite", AnimatorControllerParameterType.Trigger);

        // Nettoyage : anciens états isolés de la pêche.
        foreach (string old in OldStates)
        {
            ChildAnimatorState child = machine.states.FirstOrDefault(s => s.state.name == old);
            if (child.state != null) machine.RemoveState(child.state);
        }

        Vector3 origin = new Vector3(600, -450, 0);
        AnimatorState start = GetOrCreateState(machine, "Fishing_Tree", "FISHING_START", origin);
        AnimatorState idle = GetOrCreateState(machine, "FISHING_IDLE", "FISHING_IDLE", origin + new Vector3(260, 0, 0));
        AnimatorState struggle = GetOrCreateState(machine, "FISHING_STRUGGLE", "FISHING_STRUGGLE", origin + new Vector3(520, 90, 0));
        AnimatorState quit = GetOrCreateState(machine, "FISHING_QUIT", "FISHING_QUIT", origin + new Vector3(260, -120, 0));
        AnimatorState show = GetOrCreateState(machine, "FISHING_SHOW", "FISHING_SHOW", origin + new Vector3(520, -120, 0));

        // Tag lu par PlayerFishing pour savoir quand l'animation de fin est terminée.
        foreach (AnimatorState s in new[] { start, idle, struggle, quit, show }) s.tag = "Fishing";

        SetTree(controller, start, "Fishing START", dir => $"{FishingFolder}/Fishing_START_{dir}.anim");
        SetTree(controller, idle, "Fishing IDLE", dir => $"{FishingFolder}/Fishing_IDLE_{dir}.anim");
        SetTree(controller, struggle, "Fishing STRUGGLE", dir => $"{FishingFolder}/Fishing_STRUGGLE_{dir}.anim");
        SetTree(controller, quit, "Fishing QUIT", dir => $"{FishingFolder}/Fishing_QUIT_{dir}.anim");
        SetTree(controller, show, "Fishing SHOW", dir => $"{ShowFolder}/Show_{dir}.anim");

        foreach (AnimatorState s in new[] { start, idle, struggle, quit, show })
            foreach (AnimatorStateTransition t in s.transitions.ToArray()) s.RemoveTransition(t);

        // START → IDLE à la fin du clip.
        AddExitTimeTransition(start, idle, 1f, 0f);

        // IDLE → STRUGGLE (poisson qui mord), SHOW (poisson pris), QUIT (fin de la pêche).
        AddConditionTransition(idle, struggle, AnimatorConditionMode.If, "FishBite");
        AddConditionTransition(idle, show, AnimatorConditionMode.If, "FishCaught");
        AddConditionTransition(idle, quit, AnimatorConditionMode.IfNot, "IsFishing");

        // STRUGGLE → SHOW (pris) ou QUIT (le poisson s'échappe).
        AddConditionTransition(struggle, show, AnimatorConditionMode.If, "FishCaught");
        AddConditionTransition(struggle, quit, AnimatorConditionMode.IfNot, "IsFishing");

        // QUIT et SHOW rendent la main à IDLE_Tree en fin de clip.
        AddExitTimeTransition(quit, idleTree, 1f, 0.1f);
        AddExitTimeTransition(show, idleTree, 1f, 0.1f);

        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssets();
        Debug.Log("[FishingAnimatorBuilder] Animator de pêche câblé (START, IDLE, STRUGGLE, QUIT, SHOW x 4 directions).");
    }

    private static AnimatorStateMachine FindMachineWithState(AnimatorController controller, string stateName)
    {
        foreach (AnimatorControllerLayer layer in controller.layers)
        {
            AnimatorStateMachine found = Search(layer.stateMachine, stateName);
            if (found != null) return found;
        }
        return null;
    }

    private static AnimatorStateMachine Search(AnimatorStateMachine machine, string stateName)
    {
        if (machine.states.Any(s => s.state.name == stateName)) return machine;
        foreach (ChildAnimatorStateMachine sub in machine.stateMachines)
        {
            AnimatorStateMachine found = Search(sub.stateMachine, stateName);
            if (found != null) return found;
        }
        return null;
    }

    private static void EnsureParameter(AnimatorController controller, string name, AnimatorControllerParameterType type)
    {
        if (controller.parameters.Any(p => p.name == name)) return;
        controller.AddParameter(name, type);
    }

    /// <summary>
    /// Récupère l'état (en le renommant si besoin) ou le crée.
    /// </summary>
    private static AnimatorState GetOrCreateState(AnimatorStateMachine machine, string existingName, string newName, Vector3 position)
    {
        ChildAnimatorState child = machine.states.FirstOrDefault(s => s.state.name == existingName || s.state.name == newName);
        AnimatorState state = child.state != null ? child.state : machine.AddState(newName, position);
        state.name = newName;
        state.writeDefaultValues = true;

        ChildAnimatorState[] states = machine.states;
        for (int i = 0; i < states.Length; i++)
        {
            if (states[i].state != state) continue;
            states[i].position = position;
            machine.states = states;
            break;
        }
        return state;
    }

    private static void SetTree(AnimatorController controller, AnimatorState state, string treeName, System.Func<string, string> clipPath)
    {
        // Ancien arbre de cet état : retiré de l'asset pour ne pas laisser d'objets orphelins.
        if (state.motion is BlendTree old)
        {
            state.motion = null;
            AssetDatabase.RemoveObjectFromAsset(old);
            Object.DestroyImmediate(old, true);
        }

        BlendTree tree = new BlendTree
        {
            name = treeName,
            blendType = BlendTreeType.SimpleDirectional2D,
            blendParameter = "Dir_x",
            blendParameterY = "Dir_y",
            useAutomaticThresholds = true,
        };
        AssetDatabase.AddObjectToAsset(tree, controller);

        foreach ((string dir, Vector2 pos) in Directions)
        {
            string path = clipPath(dir);
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) { Debug.LogError($"[FishingAnimatorBuilder] Clip manquant : {path}"); continue; }
            tree.AddChild(clip, pos);
        }
        state.motion = tree;
    }

    private static void AddExitTimeTransition(AnimatorState from, AnimatorState to, float exitTime, float duration)
    {
        AnimatorStateTransition t = from.AddTransition(to);
        t.hasExitTime = true;
        t.exitTime = exitTime;
        t.hasFixedDuration = true;
        t.duration = duration;
    }

    private static void AddConditionTransition(AnimatorState from, AnimatorState to, AnimatorConditionMode mode, string parameter)
    {
        AnimatorStateTransition t = from.AddTransition(to);
        t.hasExitTime = false;
        t.hasFixedDuration = true;
        t.duration = 0f;
        t.AddCondition(mode, 0f, parameter);
    }
}
