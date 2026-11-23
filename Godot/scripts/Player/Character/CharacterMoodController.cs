using Godot;

/// <summary>
/// Humeur du personnage affichée dans le portrait (port de CharacterMoodController.cs) : une humeur « ambiante »
/// durable, et des réactions temporaires qui reviennent ensuite à l'ambiante.
/// Rien dans le jeu ne l'appelle encore (c'était déjà le cas dans Unity) : il attend qu'un système (pêche réussie,
/// récolte, nuit...) appelle SetAmbient / React. En attendant, l'humeur de départ se règle dans l'Inspecteur.
/// </summary>
public partial class CharacterMoodController : Node
{
    [Export] private UICharacterAnimator _uiCharacterAnimator;

    [ExportGroup("Debug")]
    /// <summary>Humeur appliquée au lancement (Idle : rien). Équivalent du menu « TestSetHappy » d'Unity.</summary>
    [Export] private EmotionType _debugEmotion = EmotionType.Idle;
    [Export(PropertyHint.Range, "0,3")] private int _debugIntensity;

    private SceneTreeTimer _reactTimer;
    private EmotionType _ambientEmotion = EmotionType.Idle;

    public override void _Ready()
    {
        if (_debugEmotion != EmotionType.Idle) SetAmbient(_debugEmotion, _debugIntensity);
    }

    public void SetAmbient(EmotionType emotion, int intensity = 0)
    {
        _ambientEmotion = emotion;
        // Pas pendant une réaction : elle rendra l'humeur ambiante à sa fin.
        if (_reactTimer == null) _uiCharacterAnimator.SetEmotion(emotion, intensity);
    }

    /// <summary>Joue une humeur pendant <paramref name="duration"/> secondes, puis revient à l'ambiante.</summary>
    public void React(EmotionType emotion, float duration, int intensity = 0)
    {
        _uiCharacterAnimator.SetEmotion(emotion, intensity);

        // Un nouveau minuteur remplace le précédent (StopCoroutine dans Unity) : seul le dernier compte.
        SceneTreeTimer timer = GetTree().CreateTimer(duration);
        _reactTimer = timer;
        timer.Timeout += () =>
        {
            if (_reactTimer != timer) return;
            _reactTimer = null;
            SetAmbient(_ambientEmotion);
        };
    }
}
