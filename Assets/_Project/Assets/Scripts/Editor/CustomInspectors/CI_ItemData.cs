using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

[CustomEditor(typeof(ItemData), true)]
public class CI_ItemData : Editor
{
    private const string WorldSpritePropertyName = "WorldSprite";
    private const int PreviewSize = 64;

    private Image _previewImage;

    public override VisualElement CreateInspectorGUI()
    {
        VisualElement root = new VisualElement();

        SerializedProperty worldSpriteProperty = serializedObject.FindProperty(WorldSpritePropertyName);

        VisualElement previewContainer = new VisualElement
        {
            style =
            {
                width = PreviewSize,
                height = PreviewSize,
                marginBottom = 8,
                marginTop = 4,
                marginLeft = 0,
                backgroundColor = new Color(0.08f, 0.08f, 0.08f),
                justifyContent = Justify.Center,
                alignItems = Align.Center,
                borderTopWidth = 1,
                borderBottomWidth = 1,
                borderLeftWidth = 1,
                borderRightWidth = 1,
                borderTopColor = new Color(0.25f, 0.25f, 0.25f),
                borderBottomColor = new Color(0.25f, 0.25f, 0.25f),
                borderLeftColor = new Color(0.25f, 0.25f, 0.25f),
                borderRightColor = new Color(0.25f, 0.25f, 0.25f)
            }
        };

        _previewImage = new Image
        {
            scaleMode = ScaleMode.ScaleToFit,
            pickingMode = PickingMode.Ignore,
            style =
            {
                width = PreviewSize,
                height = PreviewSize
            }
        };

        previewContainer.Add(_previewImage);
        root.Add(previewContainer);

        if (worldSpriteProperty != null)
        {
            UpdatePreview(worldSpriteProperty);

            root.TrackPropertyValue(worldSpriteProperty, property =>
            {
                UpdatePreview(property);
            });
        }
        else
        {
            previewContainer.style.display = DisplayStyle.None;
        }

        InspectorElement.FillDefaultInspector(root, serializedObject, this);

        return root;
    }

    private void UpdatePreview(SerializedProperty worldSpriteProperty)
    {
        Sprite sprite = worldSpriteProperty.objectReferenceValue as Sprite;

        _previewImage.sprite = sprite;
        _previewImage.style.display = sprite != null ? DisplayStyle.Flex : DisplayStyle.None;
    }
}