using DG.Tweening;
using UnityEngine;

public class MenuSelector : MonoBehaviour
{
    [SerializeField] private RectTransform _selectorRect;

    [Header("Animation Settings")] 
    [SerializeField] private float _duration = 0.15f;
    [SerializeField] private float _shrinkScale = 0.8f;
    [SerializeField] private Ease _easeIn = Ease.InBounce;
    [SerializeField] private Ease _easeOut = Ease.OutBounce;

    public void AnimateSelection(RectTransform clickedSlot)
    {
        _selectorRect.DOKill();
        _selectorRect.position = clickedSlot.position;

        Sequence anim = DOTween.Sequence();

        anim.Append(_selectorRect.DOScale(new Vector3(_shrinkScale, _shrinkScale, 1), _duration)
            .SetEase(_easeIn));

        anim.Append(_selectorRect.DOScale(Vector3.one, _duration)
            .SetEase(_easeOut));
    }

}
