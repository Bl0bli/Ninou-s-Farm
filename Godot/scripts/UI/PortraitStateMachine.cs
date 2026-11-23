using System;
using System.Collections.Generic;

public enum EmotionType
{
    Idle = 0, Angry = 1, Stylish = 2, Sleep = 3, Love = 4, Shy = 5, Happy = 6
}

/// <summary>
/// Machine à états du portrait : transcription de l'Animator UserCharacter.controller d'Unity (36 clips, 7 sous-machines
/// d'humeur). Logique pure, sans dépendance au moteur : elle dit QUEL clip jouer, la vue (UICharacterAnimator) le joue.
///
/// Règles reprises de l'Animator :
///  - une transition « avec temps de sortie » (ExitTime) ne part qu'une fois ce point du clip atteint ;
///    sans temps de sortie, elle part dès que sa condition est vraie ;
///  - les transitions d'un état sont essayées dans l'ordre, la première valide gagne ;
///  - « Any State » (Surprised, Yap, Quit, ComingUp) passe avant tout, depuis n'importe quel état ;
///  - sortir d'une sous-machine (Exit) ré-entre dans celle de l'humeur courante (le nœud Entry du layer) ;
///  - un déclencheur (Trigger) reste armé jusqu'à ce qu'une transition le consomme.
///
/// Écarts : pas de fondu entre clips (durées de transition ignorées) ; le point de sortie d'un clip qui boucle
/// est testé une fois par boucle, quand la lecture le franchit.
/// </summary>
public class PortraitStateMachine
{
    private const string Exit = "<exit>";
    private const float NoExitTime = -1f;

    private delegate bool Condition(PortraitStateMachine m);
    private readonly record struct Transition(string Target, Condition When, float ExitTime);
    private sealed record State(string Clip, Transition[] Transitions);

    private readonly Dictionary<string, State> _states = new();
    private readonly List<Transition> _anyState = new();
    private readonly Func<string, bool> _isLooping;

    private string _current;
    private float _lastProgress;

    // Paramètres de l'Animator
    public EmotionType Emotion { get; set; } = EmotionType.Idle;
    public int Intensity { get; set; }
    public bool Yapping { get; set; }
    private bool _surprised, _quit, _comingUp;

    public void TriggerSurprised() => _surprised = true;
    public void TriggerQuit() => _quit = true;
    public void TriggerComingUp() => _comingUp = true;

    /// <summary>Clip à jouer en ce moment (nom de l'animation dans portrait_frames.tres).</summary>
    public string CurrentClip => _states[_current].Clip;

    /// <summary>Déclenché à chaque changement d'état, avec le nom du nouveau clip (même clip rejoué compris).</summary>
    public event Action<string> ClipChanged;

    /// <param name="isLooping">Dit si un clip boucle (le point de sortie ne se teste pas pareil).</param>
    public PortraitStateMachine(Func<string, bool> isLooping)
    {
        _isLooping = isLooping;
        Build();
        _current = EntryFor(Emotion);
    }

    // ------------------------------------------------------------------ Mise à jour
    /// <summary>
    /// À appeler à chaque image avec l'avancement du clip courant (0 à 1 dans le cycle en cours ;
    /// 1 quand un clip qui ne boucle pas est terminé).
    /// </summary>
    public void Update(float progress)
    {
        // Any State : essayé avant les transitions de l'état courant.
        foreach (Transition t in _anyState)
        {
            if (!t.When(this)) continue;

            // Se retrouver soi-même (CanTransitionToSelf = non) : on arrête là, sans essayer les suivantes,
            // sinon « AngryYap » rebasculerait aussitôt sur le « Yap » générique.
            ConsumeTrigger(t.Target);
            if (t.Target != _current) Go(t.Target);
            _lastProgress = 0f;
            return;
        }

        bool loops = _isLooping(CurrentClip);
        foreach (Transition t in _states[_current].Transitions)
        {
            if (!t.When(this) || !ExitTimeReached(t.ExitTime, progress, loops)) continue;
            Go(t.Target);
            _lastProgress = 0f;
            return;
        }
        _lastProgress = progress;
    }

    /// <summary>
    /// Clip qui ne boucle pas : le temps continue de passer après sa fin, le point de sortie reste atteint (progress >= ExitTime).
    /// Clip qui boucle : le point de sortie compte au moment où la lecture le franchit dans un cycle.
    /// </summary>
    private bool ExitTimeReached(float exitTime, float progress, bool loops)
    {
        if (exitTime < 0f) return true;
        if (!loops) return progress >= exitTime;

        bool wrapped = progress < _lastProgress;
        return wrapped ? exitTime > _lastProgress || exitTime <= progress
                       : _lastProgress < exitTime && exitTime <= progress;
    }

    private void Go(string target)
    {
        _current = target == Exit ? EntryFor(Emotion) : target;
        ClipChanged?.Invoke(CurrentClip);
    }

    private void ConsumeTrigger(string target)
    {
        if (target == "Surprised") _surprised = false;
        else if (target == "Quit") _quit = false;
        else if (target == "ComingUp") _comingUp = false;
    }

    /// <summary>Le nœud Entry du layer : première étape de la sous-machine de l'humeur.</summary>
    private static string EntryFor(EmotionType emotion) => emotion switch
    {
        EmotionType.Angry => "Angry_Start",
        EmotionType.Stylish => "Stylish_Start",
        EmotionType.Sleep => "Sleepy_Loop",
        EmotionType.Love => "Love_Start",
        EmotionType.Shy => "Shy_Start",
        EmotionType.Happy => "Happy_Start",
        _ => "Ears",
    };

    // ------------------------------------------------------------------ Définition des états
    private static Condition Always => _ => true;
    private static Condition Emo(EmotionType e) => m => m.Emotion == e;
    private static Condition NotEmo(EmotionType e) => m => m.Emotion != e;
    private static Condition IntensityBelow1 => m => m.Intensity < 1;
    private static Condition IntensityAbove0 => m => m.Intensity > 0;
    private static Condition IntensityIs(int v) => m => m.Intensity == v;

    private static Transition To(string target, Condition when = null, float exitTime = NoExitTime) =>
        new(target, when ?? Always, exitTime);

    /// <summary>Ajoute un état. Identifiant = nom du clip, sauf quand deux états jouent le même clip.</summary>
    private void Add(string id, params Transition[] transitions) => AddClip(id, id.ToLowerInvariant(), transitions);

    private void AddClip(string id, string clip, params Transition[] transitions) =>
        _states[id] = new State(clip, transitions);

    private void Build()
    {
        // --- Any State, dans l'ordre de l'Animator
        _anyState.Add(To("Surprised", m => m._surprised));
        _anyState.Add(To("AngryYap_Loop", m => m.Yapping && m.Emotion == EmotionType.Angry));
        _anyState.Add(To("StylishYap_loop", m => m.Yapping && m.Emotion == EmotionType.Stylish));
        _anyState.Add(To("ShyYap_Loop", m => m.Yapping && m.Emotion == EmotionType.Shy));
        _anyState.Add(To("Yap", m => m.Yapping));
        _anyState.Add(To("Quit", m => m._quit));
        _anyState.Add(To("ComingUp", m => m._comingUp));

        // --- Niveau racine
        Add("ComingUp",
            To("Angry_Start", Emo(EmotionType.Angry)), To("Stylish_Start", Emo(EmotionType.Stylish)),
            To("Sleepy_Loop", Emo(EmotionType.Sleep)), To("Love_Start", Emo(EmotionType.Love)),
            To("Shy_Start", Emo(EmotionType.Shy)), To("Happy_Start", Emo(EmotionType.Happy)),
            To("Ears", exitTime: 0.84375f));
        Add("Yap", To(Exit, m => !m.Yapping, 0.28571433f));
        Add("Quit", To(Exit, exitTime: 0.73214287f));
        Add("ShyYap_Loop", To(Exit, m => !m.Yapping, 0.516129f));
        Add("StylishYap_loop", To(Exit, m => !m.Yapping, 0.28571433f));
        Add("AngryYap_Loop", To(Exit, m => !m.Yapping, 0.516129f));
        Add("Surprised", To(Exit, exitTime: 0.75409836f));

        // --- Idle (« Tired » et « Blink » n'ont aucune transition entrante dans l'Animator : jamais joués)
        Add("Ears", To(Exit, NotEmo(EmotionType.Idle), 0.516129f));

        // --- Angry
        Add("Angry_Start", To("Angry_Loop", exitTime: 1f));
        Add("Angry_Loop", To("Angry_End", NotEmo(EmotionType.Angry), 0.63414633f));
        Add("Angry_End", To(Exit, exitTime: 1f));

        // --- Stylish
        Add("Stylish_Start", To("Stylish_Loop", IntensityBelow1, 1f), To("VeryStylish_Loop", IntensityAbove0, 1f));
        Add("Stylish_Loop", To("Stylish_End", NotEmo(EmotionType.Stylish), 0.63414633f),
            To("VeryStylish_Loop", IntensityAbove0, 0.63414633f));
        Add("VeryStylish_Loop", To("Stylish_End", NotEmo(EmotionType.Stylish), 0.516129f),
            To("Stylish_Loop", IntensityBelow1, 0.516129f));
        Add("Stylish_End", To(Exit, exitTime: 1f));

        // --- Sleep. L'ordre des transitions est celui de l'Animator, y compris ses bizarreries : depuis
        // Sleep_Loop et VerySleep_Loop, la transition sans condition vers « _End » passe avant celles d'intensité.
        Add("Sleepy_Loop", To("Nap_Loop", IntensityIs(1), 1f), To("Sleep_Start", IntensityIs(2), 1f),
            To("VerySleep_Start", IntensityIs(3), 1f), To("SleepSurprised", NotEmo(EmotionType.Sleep), 0.83516484f));
        Add("Sleep_Start", To("Sleep_Loop", exitTime: 1f));
        Add("Sleep_Loop", To("Sleep_End", exitTime: 0.75409836f), To("Nap_Loop", IntensityIs(1), 0.75409836f),
            To("VerySleep_Loop", IntensityIs(3), 0.75409836f));
        Add("Sleep_End", To(Exit, NotEmo(EmotionType.Sleep), 1f));
        Add("Nap_Loop", To("VerySleep_Loop", IntensityIs(3), 0.87603307f), To("Sleep_Loop", IntensityIs(2), 0.87603307f),
            To("SleepSurprised", NotEmo(EmotionType.Sleep), 0.87603307f));
        Add("VerySleep_Start", To("VerySleep_Loop", exitTime: 1f));
        Add("VerySleep_Loop", To("Nap_Loop", IntensityIs(1), 0.87603307f), To("VerySleep_End", exitTime: 0.87603307f),
            To("Sleep_Loop", IntensityIs(2), 0.87603307f));
        Add("VerySleep_End", To(Exit, NotEmo(EmotionType.Sleep), 1f));
        AddClip("SleepSurprised", "surprised", To(Exit, exitTime: 1f)); // l'état « Surprised » de la sous-machine Sleep

        // --- Love
        Add("Love_Start", To("Love_Loop", exitTime: 1f));
        Add("Love_Loop", To("Love_End", NotEmo(EmotionType.Love), 0.63414633f));
        Add("Love_End", To(Exit, exitTime: 1f));

        // --- Shy
        Add("Shy_Start", To("Shy_Loop", IntensityBelow1, 1f), To("VeryShy_Loop", IntensityAbove0, 1f));
        Add("Shy_Loop", To("Shy_End", NotEmo(EmotionType.Shy), 0.63414633f), To("VeryShy_Loop", IntensityAbove0, 0.63414633f));
        Add("VeryShy_Loop", To("Shy_End", NotEmo(EmotionType.Shy), 0.516129f), To("Shy_Loop", IntensityBelow1, 0.516129f));
        Add("Shy_End", To(Exit, exitTime: 1f));

        // --- Happy
        Add("Happy_Start", To("Happy_Loop1", IntensityBelow1, 1f), To("Happy_Loop2", IntensityAbove0, 1f));
        Add("Happy_Loop1", To("Happy_End", NotEmo(EmotionType.Happy), 0.48275864f), To("Happy_Loop2", IntensityAbove0, 0.48275864f));
        Add("Happy_Loop2", To("Happy_End", NotEmo(EmotionType.Happy), 0.516129f), To("Happy_Loop1", IntensityBelow1, 0.516129f));
        Add("Happy_End", To(Exit, exitTime: 1f));
    }
}
