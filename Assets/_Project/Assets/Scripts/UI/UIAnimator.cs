using System;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;

public enum AnimType
{
    SHRINK,
    SCALE_UP,
    MOVE
}
public class UIAnimator : MonoBehaviour
{
    [SerializeField] private RectTransform _rect;
    [Header("Animation Settings")] 
    [SerializeField] private AnimType _animType = AnimType.MOVE;
    [SerializeField] private float _duration = 0.15f;
    [SerializeField, ShowIf("_animType", AnimType.SHRINK)] private float _shrinkScale = 0.8f;
    [SerializeField, ShowIf("_animType", AnimType.SCALE_UP)] private float _upScale = 1.2f;
    [SerializeField, ShowIf("_animType", AnimType.MOVE)] private float xdeltaPosition = 0.1f;
    [SerializeField, ShowIf("_animType", AnimType.MOVE)] private float ydeltaPosition = 0.1f;
    [SerializeField] private Ease _easeIn = Ease.InBounce;
    [SerializeField] private Ease _easeOut = Ease.OutBounce;
    
    private Vector3 _baseScale;
    private Vector2 _basePosition;
    private bool _isInitialized = false;

    private void Awake()
    {
        if (_rect == null) _rect = GetComponent<RectTransform>();
    }

    public void Animate()
    {
        if (!_isInitialized)
        {
            _baseScale = _rect.localScale;
            _basePosition = _rect.anchoredPosition;
            _isInitialized = true;
        }
        
        _rect.DOKill();
        
        Sequence seq = DOTween.Sequence();
        float halfDuration = _duration / 2f;

        switch (_animType)
        {
            case AnimType.SHRINK:
                seq.Append(_rect.DOScale(_baseScale * _shrinkScale, halfDuration).SetEase(_easeIn));
                seq.Append(_rect.DOScale(_baseScale, halfDuration).SetEase(_easeOut));
                break;

            case AnimType.SCALE_UP:
                seq.Append(_rect.DOScale(_baseScale * _upScale, halfDuration).SetEase(_easeIn));
                seq.Append(_rect.DOScale(_baseScale, halfDuration).SetEase(_easeOut));
                break;

            case AnimType.MOVE:
                Vector2 targetPos = _basePosition + new Vector2(xdeltaPosition, ydeltaPosition);
                seq.Append(_rect.DOAnchorPos(targetPos, halfDuration).SetEase(_easeIn));
                seq.Append(_rect.DOAnchorPos(_basePosition, halfDuration).SetEase(_easeOut));
                break;
        }
    }
}
