using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class RuleTileGenerator : EditorWindow
{
    private RuleTile _tileTemplate;
    private Texture2D _text;
    private string _fileName;

    [MenuItem("Window/Custom/RuleTile Generator")]
    public static void ShowWindow()
    {
        GetWindow<RuleTileGenerator>("RuleTile Generator");
    }

    private void OnGUI()
    {
        GUILayout.Label("Configuration", EditorStyles.boldLabel);

        _fileName = GUILayout.TextField(_fileName, 256); 
        _tileTemplate = (RuleTile)EditorGUILayout.ObjectField("Template Base", _tileTemplate, typeof(RuleTile), false);
        _text = (Texture2D)EditorGUILayout.ObjectField("Tilese", _text, typeof(Texture2D), false);

        if (GUILayout.Button("Generate"))
        {
            GenerateRuleTile();
        }
    }

    public void GenerateRuleTile()
    {
        if (_tileTemplate == null)
        {
            Debug.LogError("Tile Template is null, can't generate, please select a template");
            return;
        }

        if (_text == null)
        {
            Debug.LogError("Can't Generate, Texture is null");
            return;
        }

        string texturePath = AssetDatabase.GetAssetPath(_text);
        
        string directory = System.IO.Path.GetDirectoryName(AssetDatabase.GetAssetPath(_tileTemplate));
        string name = string.IsNullOrEmpty(_fileName) ? _text.name + "_Generated" : _fileName;
        string finalPath = AssetDatabase.GenerateUniqueAssetPath($"{directory}/{name}.asset");

        Object[] assets = AssetDatabase.LoadAllAssetsAtPath(texturePath);
        List<Sprite> sprites = assets.OfType<Sprite>()
            .Where(s => Regex.IsMatch(s.name, @"_\d+$"))
            .OrderBy(s => ExtractIDNumber(s.name)).ToList();

        Dictionary<int, Sprite> spriteDict = new Dictionary<int, Sprite>();

        foreach (Sprite s in sprites)
        {
            int id = ExtractIDNumber(s.name);
            if (id != -1 && !spriteDict.ContainsKey(id))
            {
                spriteDict.Add(id, s);
            }
        }
        
        RuleTile newTile = ScriptableObject.CreateInstance<RuleTile>();
        if (_tileTemplate.m_DefaultSprite != null)
        {
            int defaultId = ExtractIDNumber(_tileTemplate.m_DefaultSprite.name);
            if (spriteDict.ContainsKey(defaultId)) newTile.m_DefaultSprite = spriteDict[defaultId];
        }

        foreach (RuleTile.TilingRule rule in _tileTemplate.m_TilingRules)
        {
            RuleTile.TilingRule newRule = new RuleTile.TilingRule();
            newRule.m_Neighbors = rule.m_Neighbors;
            newRule.m_NeighborPositions = rule.m_NeighborPositions;
            newRule.m_Output = rule.m_Output;
            newRule.m_ColliderType = rule.m_ColliderType;

            if (rule.m_Sprites != null && rule.m_Sprites.Length > 0)
            {
                List<Sprite> newRuleSprites = new List<Sprite>();
                
                foreach (Sprite originalSprite in rule.m_Sprites)
                {
                    if (originalSprite != null)
                    {
                        int originalSpriteId = ExtractIDNumber(originalSprite.name);

                        if (spriteDict.ContainsKey(originalSpriteId))
                        {
                            newRuleSprites.Add(spriteDict[originalSpriteId]);
                        }
                        else
                        {
                            Debug.LogWarning($"Attention : Le sprite se terminant par {originalSpriteId} est requis, mais introuvable !");
                        }
                    }
                }
                
                newRule.m_Sprites = newRuleSprites.ToArray();
            }
            
            newTile.m_TilingRules.Add(newRule);
        }
        
        AssetDatabase.CreateAsset(newTile, finalPath);
        AssetDatabase.SaveAssets();
        
        Selection.activeObject = newTile;
        Debug.Log($"Succès : RuleTile sauvegardé sous {finalPath}");
    }

    private static int  ExtractIDNumber(string name)
    {
        Match match = Regex.Match(name, @"\d+$"); // prend les derniers chiffres de la chaine de caracteres

        if (match.Success)
        {
            return int.Parse(match.Value);
        }

        return -1;
    }
}
