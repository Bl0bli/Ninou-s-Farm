using System.Collections;
using UnityEngine;

public class CharacterMoodController : MonoBehaviour
{
    [SerializeField] private UICharacterAnimator _uiCharacterAnimator;
    
    [Header("Debug")]
    [SerializeField] private EmotionType _debugEmotion;
    [SerializeField] private int _debugIntensity;

    private Coroutine _reactRoutine;
    private EmotionType _ambientEmotion = EmotionType.Idle;

    public void SetAmbient(EmotionType emotion, int intensity = 0)
    {
        _ambientEmotion = emotion;
        if(_reactRoutine == null)
        {
            _uiCharacterAnimator.SetEmotion(emotion, intensity);
        }
    }

    public void React(EmotionType emotion, float duration, int intensity = 0)
    {
        if(_reactRoutine != null)
        {
            StopCoroutine(_reactRoutine);
        }
        _reactRoutine = StartCoroutine(ReactRoutine(emotion, duration, intensity));
    }
    
    [ContextMenu("TestSetHappy")]
    public void Test() => SetAmbient(_debugEmotion, _debugIntensity);
    
    private IEnumerator ReactRoutine(EmotionType emotion, float duration, int intensity = 0)
    {
        _uiCharacterAnimator.SetEmotion(emotion, intensity);
        yield return new WaitForSeconds(duration);
        _reactRoutine = null;
        SetAmbient(_ambientEmotion);
    }
    
    
}
