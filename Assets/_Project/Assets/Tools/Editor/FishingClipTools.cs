using UnityEditor;
using UnityEngine;

/// <summary>
/// Génère les clips de pêche « Right » à partir des clips « Left » : mêmes sprites, plus une courbe
/// SpriteRenderer.flipX = 1. Le retournement est écrit par AnimationUtility, pas à la main dans le YAML.
/// </summary>
public static class FishingClipTools
{
    private static readonly string[] ClipFolders =
    {
        "Assets/_Project/Assets/Characters/Pneem/Fishing",
        "Assets/_Project/Assets/Characters/Pneem/Show",
    };

    [MenuItem("Tools/Ninou's Farm/Fishing/Build Right Clips From Left")]
    public static void BuildRightClips()
    {
        int built = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:AnimationClip", ClipFolders))
        {
            string leftPath = AssetDatabase.GUIDToAssetPath(guid);
            if (!leftPath.EndsWith("_Left.anim")) continue;

            AnimationClip left = AssetDatabase.LoadAssetAtPath<AnimationClip>(leftPath);
            string rightPath = leftPath.Replace("_Left.anim", "_Right.anim");
            AnimationClip right = AssetDatabase.LoadAssetAtPath<AnimationClip>(rightPath);

            // Le fichier existant est conservé (son GUID est référencé par l'Animator).
            if (right == null)
            {
                right = new AnimationClip();
                AssetDatabase.CreateAsset(right, rightPath);
            }

            right.ClearCurves();
            foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(left))
                AnimationUtility.SetObjectReferenceCurve(right, binding, AnimationUtility.GetObjectReferenceCurve(left, binding));

            EditorCurveBinding flip = EditorCurveBinding.FloatCurve("", typeof(SpriteRenderer), "m_FlipX");
            AnimationUtility.SetEditorCurve(right, flip, AnimationCurve.Constant(0f, left.length, 1f));

            right.frameRate = left.frameRate;
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(left);
            AnimationUtility.SetAnimationClipSettings(right, settings);

            EditorUtility.SetDirty(right);
            built++;
        }

        AssetDatabase.SaveAssets();
        Debug.Log($"[FishingClipTools] {built} clip(s) Right régénéré(s) depuis Left (flipX = 1).");
    }
}
