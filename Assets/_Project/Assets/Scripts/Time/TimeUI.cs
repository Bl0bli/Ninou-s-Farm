using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

public class TimeUI : MonoBehaviour
{
    private enum SkyPhase { Day, Sunset, Night }

    [System.Serializable]
    private struct SkyKeyframe
    {
        [Range(0f, 24f)] public float Hour;
        public SkyPhase Phase;
    }

    [System.Serializable]
    private struct ArrowKeyframe
    {
        [Range(0f, 24f)] public float Hour;
        public float Angle;
    }

    [Header("UI Refs")]
    [SerializeField] private Image _dayImage;
    [SerializeField] private Image _sunsetImage;
    [SerializeField] private Image _nightImage;
    [Header("Cycle")]
    [Tooltip("Heures triées par ordre croissant. Le cycle boucle sur 24h : " +
             "entre deux keyframes de même phase le ciel est stable, sinon il fond de l'une vers l'autre.")]
    [SerializeField]
    private SkyKeyframe[] _keyframes =
    {
        new SkyKeyframe { Hour = 6f,  Phase = SkyPhase.Night },
        new SkyKeyframe { Hour = 7f,  Phase = SkyPhase.Day },
        new SkyKeyframe { Hour = 16f, Phase = SkyPhase.Day },
        new SkyKeyframe { Hour = 17f, Phase = SkyPhase.Sunset },
        new SkyKeyframe { Hour = 20f, Phase = SkyPhase.Night },
    };

    [Header("Weather Arrow")]
    [Tooltip("Le pivot parent de Weather_Arrow : c'est lui qui tourne.")]
    [SerializeField] private Transform _arrowPivot;
    [SerializeField] private float _arrowRotateDuration = 0.35f;
    [Tooltip("Amplitude du dépassement en fin de course (0 = pas d'à-coup).")]
    [SerializeField] private float _arrowOvershoot = 3f;
    [SerializeField]
    private ArrowKeyframe[] _arrowKeyframes =
    {
        new ArrowKeyframe { Hour = 7f,  Angle = 45f },
        new ArrowKeyframe { Hour = 17f, Angle = 0f },
        new ArrowKeyframe { Hour = 20f, Angle = -45f },
    };

    private Tween _arrowTween;
    private float _arrowAngle;
    private bool _arrowInitialized;

    private void OnEnable()
    {
        TimeManager.OnMinuteChanged += UpdateTime;
        UpdateTime();
    }

    private void OnDisable()
    {
        TimeManager.OnMinuteChanged -= UpdateTime;
        _arrowTween?.Kill();
        _arrowTween = null;
    }

    private void UpdateTime()
    {
        float hour = TimeManager.Hour + TimeManager.Minute / 60f;
        UpdateSky(hour);
        UpdateArrow(hour);
    }

    /// <summary>
    /// La flèche ne suit pas l'heure en continu : elle claque d'un coup vers l'angle
    /// de la tranche horaire courante, avec un léger dépassement (Ease.OutBack).
    /// </summary>
    private void UpdateArrow(float hour)
    {
        if (_arrowPivot == null || _arrowKeyframes == null || _arrowKeyframes.Length == 0) return;

        float target = GetArrowAngle(hour);

        // Au tout premier passage on se place sans animer, sinon la flèche
        // partirait de sa rotation d'éditeur au lancement de la scène.
        if (!_arrowInitialized)
        {
            _arrowInitialized = true;
            _arrowAngle = target;
            _arrowPivot.localRotation = Quaternion.Euler(0f, 0f, target);
            return;
        }

        if (Mathf.Approximately(target, _arrowAngle)) return;

        _arrowAngle = target;
        _arrowTween?.Kill();
        _arrowTween = _arrowPivot
            .DOLocalRotate(new Vector3(0f, 0f, target), _arrowRotateDuration)
            .SetEase(Ease.OutBack, _arrowOvershoot);
    }

    /// <summary>
    /// Angle de la dernière keyframe atteinte. Avant la première de la journée,
    /// on conserve celle de la veille (la dernière du tableau).
    /// </summary>
    private float GetArrowAngle(float hour)
    {
        int index = -1;
        for (int i = 0; i < _arrowKeyframes.Length; i++)
        {
            if (_arrowKeyframes[i].Hour <= hour) index = i;
        }

        if (index < 0) index = _arrowKeyframes.Length - 1;
        return _arrowKeyframes[index].Angle;
    }

    private void UpdateSky(float hour)
    {
        if (_keyframes == null || _keyframes.Length == 0) return;

        int next = 0;
        while (next < _keyframes.Length && _keyframes[next].Hour <= hour) next++;

        int fromIndex = (next - 1 + _keyframes.Length) % _keyframes.Length;
        int toIndex = next % _keyframes.Length;

        // Mathf.Repeat gère le passage de minuit (ex: 21h -> 5h = 8h de segment).
        float segment = Mathf.Repeat(_keyframes[toIndex].Hour - _keyframes[fromIndex].Hour, 24f);
        float elapsed = Mathf.Repeat(hour - _keyframes[fromIndex].Hour, 24f);
        float blend = segment > 0f ? Mathf.Clamp01(elapsed / segment) : 0f;

        ApplyBlend(_keyframes[fromIndex].Phase, _keyframes[toIndex].Phase, blend);
    }

    /// <summary>
    /// Fait apparaître la phase cible par-dessus la phase courante restée opaque.
    /// Empiler ainsi (au lieu de croiser deux alphas) évite l'effet "délavé"
    /// où l'on verrait à travers les deux calques au milieu du fondu.
    /// </summary>
    private void ApplyBlend(SkyPhase from, SkyPhase to, float blend)
    {
        SetAlpha(_dayImage, 0f);
        SetAlpha(_sunsetImage, 0f);
        SetAlpha(_nightImage, 0f);

        Image fromImage = GetImage(from);
        SetAlpha(fromImage, 1f);

        if (from == to) return;

        Image toImage = GetImage(to);
        if (toImage == null) return;

        BringToFrontOfBackgrounds(toImage);
        SetAlpha(toImage, blend);
    }

    /// <summary>
    /// Place l'image devant les deux autres images de ciel, sans jamais dépasser
    /// le plus haut des trois : les éléments au-dessus (UserCharacter, HUD...) restent devant.
    /// </summary>
    private void BringToFrontOfBackgrounds(Image target)
    {
        int topIndex = TopBackgroundIndex();
        if (target.transform.GetSiblingIndex() != topIndex)
            target.transform.SetSiblingIndex(topIndex);
    }

    private int TopBackgroundIndex()
    {
        int top = 0;
        if (_dayImage != null) top = Mathf.Max(top, _dayImage.transform.GetSiblingIndex());
        if (_sunsetImage != null) top = Mathf.Max(top, _sunsetImage.transform.GetSiblingIndex());
        if (_nightImage != null) top = Mathf.Max(top, _nightImage.transform.GetSiblingIndex());
        return top;
    }

    private Image GetImage(SkyPhase phase)
    {
        switch (phase)
        {
            case SkyPhase.Day: return _dayImage;
            case SkyPhase.Sunset: return _sunsetImage;
            default: return _nightImage;
        }
    }

    private static void SetAlpha(Image image, float alpha)
    {
        if (image == null) return;

        Color color = image.color;
        color.a = alpha;
        image.color = color;
        Debug.Log($"Time : {TimeManager.Hour}:{TimeManager.Minute} - Set alpha of {image.gameObject.name} to {alpha}");
    }
}
