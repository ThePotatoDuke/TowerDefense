using System.IO;
using TMPro;
using UnityEditor;
using UnityEngine;

// Assets > Create > TextMeshPro > Pixel Outline Material: with a TextMeshPro font asset selected, makes a
// material preset next to it that draws the font with a square pixel outline (TMP Pixel Outline shader).
// Pick it on a text under its Material Preset; the outline's width and color are on the material.
public static class PixelOutlineMaterial
{
    private const string ShaderName = "TowerDefense/TMP Pixel Outline";

    [MenuItem("Assets/Create/TextMeshPro/Pixel Outline Material", true)]
    private static bool CanCreate() => Selection.activeObject is TMP_FontAsset;

    [MenuItem("Assets/Create/TextMeshPro/Pixel Outline Material")]
    private static void Create()
    {
        var font = (TMP_FontAsset)Selection.activeObject;
        Shader shader = Shader.Find(ShaderName);
        if (shader == null)
        {
            Debug.LogWarning($"Pixel outline: shader {ShaderName} not found.");
            return;
        }

        // Named after the font so TextMeshPro lists it among the font's material presets
        string fontPath = AssetDatabase.GetAssetPath(font);
        string path = AssetDatabase.GenerateUniqueAssetPath(
            Path.Combine(Path.GetDirectoryName(fontPath), font.name + " Pixel Outline.mat").Replace(Path.DirectorySeparatorChar, '/'));

        var material = new Material(shader);
        material.SetTexture("_MainTex", font.atlasTexture);
        AssetDatabase.CreateAsset(material, path);
        AssetDatabase.SaveAssets();
        Selection.activeObject = material;

        if (font.atlasPadding < material.GetFloat("_Padding"))
            Debug.LogWarning($"Pixel outline: {font.name} has only {font.atlasPadding} pixels of atlas padding, less than the " +
                             "outline width, so bits of neighboring letters will show. Remake it with more padding " +
                             "(Window > TextMeshPro > Font Asset Creator).", font);
    }
}
