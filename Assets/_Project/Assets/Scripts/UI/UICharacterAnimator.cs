using UnityEngine;

public enum EmotionType
{
    Idle = 0, Angry = 1, Stylish = 2, Sleep = 3, Love = 4, Shy = 5, Happy = 6
}

public class UICharacterAnimator : MonoBehaviour
{
    [SerializeField] private Animator _animator;

    #region AnimatorParams
    private static readonly int EmotionHash = Animator.StringToHash("Emotion_Lvl");
    private static readonly int IntensityHash = Animator.StringToHash("Intensity");
    private static readonly int YappingHash   = Animator.StringToHash("Yapping");
    private static readonly int SurprisedHash = Animator.StringToHash("Surprised");
    private static readonly int QuitHash      = Animator.StringToHash("Quit");
    private static readonly int ComingUpHash  = Animator.StringToHash("ComingUp");
    #endregion
    
    public EmotionType CurrentEmotion { get; private set; } = EmotionType.Idle;

    public bool IsYapping
    {
        get => _animator.GetBool(YappingHash);
        set => _animator.SetBool(YappingHash, value);
    }

    public void SetEmotion(EmotionType emotion, int intensity = 0)
    {
        CurrentEmotion = emotion;
        _animator.SetInteger(EmotionHash, (int)emotion);
        _animator.SetInteger(IntensityHash, intensity);
    }
    
    public void Show() => _animator.SetTrigger(ComingUpHash);
    public void TriggerSurprised() => _animator.SetTrigger(SurprisedHash);
    public void Quit() => _animator.SetTrigger(QuitHash);
}
