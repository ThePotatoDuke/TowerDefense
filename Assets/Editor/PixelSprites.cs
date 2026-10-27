using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Imports pixel art as crisp sprites like the rest of the game's (16 pixels per unit, point filtered, not
// compressed), for the setup tools. Tools > Set Up Cut Grass Sprites gives the scene's GroundDecorator
// every cut_grass sprite, one picked at random for each blade a cut throws out.
public static class PixelSprites
{
    private const string EnvironmentFolder = "Assets/3rd Party/Sprites/Environment";

    // fullRect for sprites drawn tiled (a chain), which Unity wants as plain rectangles
    public static Sprite Import(string path, bool fullRect = false)
    {
        if (AssetImporter.GetAtPath(path) is not TextureImporter importer) return null;

        var settings = new TextureImporterSettings();
        importer.ReadTextureSettings(settings);
        SpriteMeshType mesh = fullRect ? SpriteMeshType.FullRect : settings.spriteMeshType;
        if (importer.textureType != TextureImporterType.Sprite || importer.spriteImportMode != SpriteImportMode.Single ||
            !Mathf.Approximately(importer.spritePixelsPerUnit, 16f) || importer.filterMode != FilterMode.Point ||
            importer.mipmapEnabled || importer.textureCompression != TextureImporterCompression.Uncompressed ||
            settings.spriteMeshType != mesh)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = mesh;
            importer.SetTextureSettings(settings);
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Sprite>(path);
    }

    [MenuItem("Tools/Set Up Cut Grass Sprites")]
    private static void SetUpCutGrass()
    {
        var decorator = Object.FindAnyObjectByType<GroundDecorator>(FindObjectsInactive.Include);
        if (decorator == null)
        {
            Debug.LogWarning("Cut grass: open the game scene first (no GroundDecorator found).");
            return;
        }

        Sprite[] sprites = Directory.GetFiles(EnvironmentFolder, "cut_grass*.png")
            .Select(file => file.Replace('\\', '/'))
            .OrderBy(file => file)
            .Select(file => Import(file))
            .Where(sprite => sprite != null)
            .ToArray();

        var serialized = new SerializedObject(decorator);
        SerializedProperty list = serialized.FindProperty("bladeSprites");
        list.arraySize = sprites.Length;
        for (int i = 0; i < sprites.Length; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = sprites[i];
        serialized.ApplyModifiedProperties();

        Debug.Log($"Cut grass: the GroundDecorator now throws out {sprites.Length} kinds of blade " +
                  $"({string.Join(", ", sprites.Select(sprite => sprite.name))}). Save the scene to keep it.");
    }
}
