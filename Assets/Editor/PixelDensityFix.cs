using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// Tools > Fix Pixel Density: makes every sprite in the prefabs and the open scene show its art at the
// game's 16 pixels per world unit, so all pixels are the same size on screen. A sprite imported at another
// pixels-per-unit, or scaled up or down, gets its own transform rescaled to match (this changes how big it
// looks, not its colliders, which sit elsewhere). Sprites inside a nested prefab are fixed in that prefab.
// Prefabs in the folders listed in Skipped are left as they are on purpose (bullets read better smaller).
// Lists everything it changed in the Console; safe to run again.
public static class PixelDensityFix
{
    private const float PixelsPerUnit = 16f;
    private const float Tolerance = 0.02f; // off by less than 2% counts as right
    private static readonly string[] Skipped = { "Assets/Prefabs/Weapons/WeaponPrefabs/Ranged/Projectiles" };

    [MenuItem("Tools/Fix Pixel Density")]
    private static void Fix()
    {
        var changes = new List<string>();

        // Prefabs first, so scene copies of them pick the fix up
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (System.Array.Exists(Skipped, folder => path.StartsWith(folder + "/"))) continue;
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            bool changed = false;
            foreach (SpriteRenderer renderer in root.GetComponentsInChildren<SpriteRenderer>(true))
            {
                // A nested prefab's sprites are fixed in that prefab itself
                if (PrefabUtility.IsPartOfPrefabInstance(renderer)) continue;
                if (FixRenderer(renderer, root.transform, out string change))
                {
                    changes.Add($"{path}: {change}");
                    changed = true;
                }
            }
            if (changed)
                PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
        }

        // Then the open scene's own sprites (copies of prefabs were fixed through the prefab)
        var sceneRenderers = Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        foreach (SpriteRenderer renderer in sceneRenderers)
        {
            if (PrefabUtility.IsPartOfPrefabInstance(renderer)) continue;
            if (FixRenderer(renderer, null, out string change, record: true))
                changes.Add($"Scene: {change}");
        }
        if (changes.Count > 0)
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        Debug.Log(changes.Count == 0
            ? "Pixel density: everything is already at 16 pixels per unit."
            : $"Pixel density: fixed {changes.Count} sprites (save the scene to keep the scene ones):\n" +
              string.Join("\n", changes));
    }

    // Rescales the sprite's own transform so it shows at 16 pixels per unit, measured up to the given root
    // (or the top of the scene)
    private static bool FixRenderer(SpriteRenderer renderer, Transform root, out string change, bool record = false)
    {
        change = null;
        Sprite sprite = renderer.sprite;
        if (sprite == null) return false;

        float scale = 1f;
        for (Transform t = renderer.transform; t != null; t = t == root ? null : t.parent)
            scale *= Mathf.Abs(t.localScale.x);
        if (scale <= 0f) return false;

        float density = sprite.pixelsPerUnit / scale;
        if (Mathf.Abs(density - PixelsPerUnit) / PixelsPerUnit < Tolerance) return false;

        if (record)
            Undo.RecordObject(renderer.transform, "Fix pixel density");
        Vector3 before = renderer.transform.localScale;
        renderer.transform.localScale = before * (density / PixelsPerUnit);
        change = $"{Path(renderer.transform)} drew at {density:0.##} px/unit; " +
                 $"scale {before.x:0.###} -> {renderer.transform.localScale.x:0.###}";
        return true;
    }

    private static string Path(Transform t) => t.parent == null ? t.name : Path(t.parent) + "/" + t.name;
}
