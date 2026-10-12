using System.Collections;
using UnityEngine;

/// <summary>
/// Joue une suite de sprites une seule fois à une position, puis se détruit.
/// Pour les petits effets (éclaboussure d'eau...) qui n'ont pas besoin d'un Animator ni d'un prefab.
/// </summary>
public class SpriteOneShot : MonoBehaviour
{
    private SpriteRenderer _renderer;

    /// <summary>
    /// Crée l'effet et le lance.
    /// </summary>
    /// <param name="frames">Images de l'effet, dans l'ordre.</param>
    /// <param name="position">Position monde de l'effet.</param>
    /// <param name="frameRate">Images par seconde.</param>
    /// <param name="delay">Attente avant la première image, en secondes.</param>
    /// <param name="sortingLayer">Nom du sorting layer du SpriteRenderer.</param>
    /// <param name="sortingOrder">Ordre dans le sorting layer.</param>
    public static void Spawn(Sprite[] frames, Vector2 position, float frameRate, float delay, string sortingLayer, int sortingOrder)
    {
        if (frames == null || frames.Length == 0 || frameRate <= 0f) return;

        GameObject go = new GameObject("SpriteOneShot");
        go.transform.position = position;

        SpriteOneShot effect = go.AddComponent<SpriteOneShot>();
        effect._renderer = go.AddComponent<SpriteRenderer>();
        effect._renderer.sortingLayerName = sortingLayer;
        effect._renderer.sortingOrder = sortingOrder;
        effect.StartCoroutine(effect.Play(frames, frameRate, delay));
    }

    private IEnumerator Play(Sprite[] frames, float frameRate, float delay)
    {
        if (delay > 0f) yield return new WaitForSeconds(delay);

        WaitForSeconds wait = new WaitForSeconds(1f / frameRate);
        foreach (Sprite frame in frames)
        {
            _renderer.sprite = frame;
            yield return wait;
        }

        Destroy(gameObject);
    }
}
