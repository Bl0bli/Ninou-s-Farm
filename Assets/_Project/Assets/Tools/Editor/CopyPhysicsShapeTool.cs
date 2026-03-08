using System;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

public class CopyPhysicsShapeTool : EditorWindow
{
    private Texture2D _source;
    private Texture2D _target;
    
    [MenuItem("Window/Custom/TileSetTool")]
    public static void ShowWindow()
    {
        GetWindow<CopyPhysicsShapeTool>("Collision copier");
    }

    private void OnGUI()
    {
        GUILayout.Label("Copier les Custom Physics Shapes", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        _source = (Texture2D)EditorGUILayout.ObjectField("Source Tileset", _source, typeof(Texture2D), false);
        _target = (Texture2D)EditorGUILayout.ObjectField("Targete Tileset", _target, typeof(Texture2D), false);
        
        EditorGUILayout.Space();
        
        if(GUILayout.Button("Copier", GUILayout.Height(30)))
        {
            if (_source != null && _target != null)
            {
                CopyPhysicsShape();
            }
            else
            {
                EditorUtility.DisplayDialog("Error", "Please assign the two textures.", "OK");
            }
        }
    }

    private void CopyPhysicsShape()
    {
        string targetPath = AssetDatabase.GetAssetPath(_target);

        var api = new SpriteDataProviderFactories();
        api.Init();
        
        var sourceDataProvider = api.GetSpriteEditorDataProviderFromObject(_source);
        sourceDataProvider.InitSpriteEditorDataProvider();
        var sourcePhysics = sourceDataProvider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
        var sourceSprites = sourceDataProvider.GetSpriteRects();
        
        var targetDataProvider = api.GetSpriteEditorDataProviderFromObject(_target);
        targetDataProvider.InitSpriteEditorDataProvider();
        var targetPhysics = targetDataProvider.GetDataProvider<ISpritePhysicsOutlineDataProvider>();
        var targetSprites = targetDataProvider.GetSpriteRects();
        
        if (sourceSprites.Length != targetSprites.Length)
        {
            EditorUtility.DisplayDialog("Erreur", "Les deux textures n'ont pas le même nombre de sprites découpés !", "Annuler");
            return;
        }
        
        for (int i = 0; i < sourceSprites.Length; i++)
        {
            var sourceGuid = sourceSprites[i].spriteID;
            var targetGuid = targetSprites[i].spriteID;

            var outlines = sourcePhysics.GetOutlines(sourceGuid);
            targetPhysics.SetOutlines(targetGuid, outlines);
            targetPhysics.SetTessellationDetail(targetGuid, sourcePhysics.GetTessellationDetail(sourceGuid));
        }
        
        targetDataProvider.Apply();
        
        AssetImporter targetImporter = AssetImporter.GetAtPath(targetPath);
        targetImporter.SaveAndReimport();

        EditorUtility.DisplayDialog("Succès", $"Les collisions ont été copiées avec succès vers {_target.name} !", "Super");
    }
}
