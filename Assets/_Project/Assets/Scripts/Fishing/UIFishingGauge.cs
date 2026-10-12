using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Jauge de pêche verticale, en overlay sur le bord droit de l'écran. N'a aucune logique :
/// elle dessine l'état d'un FishingGauge. Elle construit elle-même son Canvas, donc rien à câbler dans la scène.
/// </summary>
public class UIFishingGauge : MonoBehaviour
{
    private static readonly Color BorderColor = new Color(0.93f, 0.80f, 0.60f);
    private static readonly Color BackgroundColor = new Color(0.27f, 0.17f, 0.14f);
    private static readonly Color ZoneColor = new Color(0.52f, 0.75f, 0.40f);
    private static readonly Color CursorColor = new Color(0.96f, 0.96f, 0.90f);
    private static readonly Color HitColor = new Color(0.55f, 0.90f, 0.45f);
    private static readonly Color MissColor = new Color(0.85f, 0.30f, 0.25f);

    private const float PanelWidth = 56f;
    private const float PanelHeight = 520f;
    private const float Border = 6f;
    private const float RightMargin = 80f;

    private GameObject _root;
    private RectTransform _track;
    private RectTransform _zone;
    private RectTransform _cursor;
    private Image _cursorImage;
    private FishingGauge _gauge;
    private Coroutine _hideRoutine;

    private void Awake() => Build();

    /// <summary>
    /// Affiche la jauge et la relie à la logique : le curseur suit OnCursorMoved.
    /// </summary>
    public void Bind(FishingGauge gauge)
    {
        Unbind();
        _gauge = gauge;
        _gauge.OnCursorMoved += MoveCursor;

        if (_hideRoutine != null) StopCoroutine(_hideRoutine);
        _cursorImage.color = CursorColor;
        SetZone(gauge.ZoneMin, gauge.ZoneMax);
        MoveCursor(gauge.Cursor);
        _root.SetActive(true);
    }

    /// <summary>
    /// Colore le curseur selon le résultat puis masque la jauge après un court instant.
    /// </summary>
    public void ShowResult(bool success, float duration = 0.5f)
    {
        _cursorImage.color = success ? HitColor : MissColor;
        if (_hideRoutine != null) StopCoroutine(_hideRoutine);
        _hideRoutine = StartCoroutine(HideAfter(duration));
    }

    public void Hide()
    {
        if (_hideRoutine != null) StopCoroutine(_hideRoutine);
        Unbind();
        _root.SetActive(false);
    }

    private IEnumerator HideAfter(float duration)
    {
        yield return new WaitForSeconds(duration);
        Hide();
    }

    private void Unbind()
    {
        if (_gauge != null) _gauge.OnCursorMoved -= MoveCursor;
        _gauge = null;
    }

    private void OnDestroy() => Unbind();

    private void MoveCursor(float value) => _cursor.anchorMin = _cursor.anchorMax = new Vector2(0.5f, Mathf.Clamp01(value));

    private void SetZone(float min, float max)
    {
        _zone.anchorMin = new Vector2(0f, min);
        _zone.anchorMax = new Vector2(1f, max);
        _zone.offsetMin = _zone.offsetMax = Vector2.zero;
    }

    private void Build()
    {
        _root = new GameObject("FishingGaugeCanvas", typeof(Canvas), typeof(CanvasScaler));
        _root.transform.SetParent(transform, false);

        Canvas canvas = _root.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 50;

        CanvasScaler scaler = _root.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        // Cadre : ancré au milieu du bord droit.
        RectTransform frame = CreateImage("Frame", _root.transform, BorderColor).rectTransform;
        frame.anchorMin = frame.anchorMax = frame.pivot = new Vector2(1f, 0.5f);
        frame.anchoredPosition = new Vector2(-RightMargin, 0f);
        frame.sizeDelta = new Vector2(PanelWidth, PanelHeight);

        // Piste : l'intérieur du cadre, c'est elle qui définit l'échelle 0 à 1.
        Image track = CreateImage("Track", frame, BackgroundColor);
        _track = track.rectTransform;
        _track.anchorMin = Vector2.zero;
        _track.anchorMax = Vector2.one;
        _track.offsetMin = new Vector2(Border, Border);
        _track.offsetMax = new Vector2(-Border, -Border);

        _zone = CreateImage("Zone", _track, ZoneColor).rectTransform;

        // Curseur : un trait horizontal un peu plus large que la piste.
        _cursorImage = CreateImage("Cursor", _track, CursorColor);
        _cursor = _cursorImage.rectTransform;
        _cursor.sizeDelta = new Vector2(PanelWidth + 8f, 8f);

        _root.SetActive(false);
    }

    private static Image CreateImage(string objectName, Transform parent, Color color)
    {
        GameObject go = new GameObject(objectName, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        Image image = go.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;
        return image;
    }
}
