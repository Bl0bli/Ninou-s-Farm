using Godot;

/// <summary>
/// Portrait animé du personnage (port d'UICharacterAnimator.cs). Même interface qu'en Unity (SetEmotion, Show,
/// TriggerSurprised, Quit, IsYapping) ; l'Animator est remplacé par PortraitStateMachine, qui choisit le clip,
/// et un AnimatedSprite2D qui le joue.
/// </summary>
public partial class UICharacterAnimator : Node
{
    [Export] private AnimatedSprite2D _sprite;

    private PortraitStateMachine _machine;
    private bool _finished;

    public EmotionType CurrentEmotion { get; private set; } = EmotionType.Idle;

    public bool IsYapping
    {
        get => _machine.Yapping;
        set => _machine.Yapping = value;
    }

    public override void _Ready()
    {
        _machine = new PortraitStateMachine(clip => _sprite.SpriteFrames.GetAnimationLoopMode(clip) != SpriteFrames.LoopMode.None);
        _machine.ClipChanged += Play;
        _sprite.AnimationFinished += () => _finished = true;
        Play(_machine.CurrentClip);
    }

    public override void _Process(double delta) => _machine.Update(GetProgress());

    public void SetEmotion(EmotionType emotion, int intensity = 0)
    {
        CurrentEmotion = emotion;
        _machine.Emotion = emotion;
        _machine.Intensity = intensity;
    }

    public void Show() => _machine.TriggerComingUp();
    public void TriggerSurprised() => _machine.TriggerSurprised();
    public void Quit() => _machine.TriggerQuit();

    private void Play(string clip)
    {
        _finished = false;
        _sprite.Play(clip);
    }

    /// <summary>
    /// Avancement dans le clip courant, de 0 à 1 (durée de chaque image comprise). Un clip qui ne boucle pas
    /// et qui est terminé reste à 1 : c'est ce que la machine attend pour les transitions « fin de clip ».
    /// </summary>
    private float GetProgress()
    {
        string clip = _sprite.Animation;
        if (_finished) return 1f;

        SpriteFrames frames = _sprite.SpriteFrames;
        int count = frames.GetFrameCount(clip);
        float total = 0f, before = 0f;
        for (int i = 0; i < count; i++)
        {
            float d = frames.GetFrameDuration(clip, i);
            if (i < _sprite.Frame) before += d;
            total += d;
        }
        if (total <= 0f) return 0f;
        float current = _sprite.Frame < count ? frames.GetFrameDuration(clip, _sprite.Frame) : 0f;
        return Mathf.Clamp((before + current * _sprite.FrameProgress) / total, 0f, 1f);
    }
}
