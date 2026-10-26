using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tools > Set Up Health Bar Prefab: makes Prefabs/UI/HealthBar.prefab (a camera-facing bar with a dark
// outline) and swaps every health bar in the enemy prefabs and the open scene for an instance of it. Each
// keeps its owner, place, bar size and colors; the bar is centered on the canvas, so it no longer swings
// out of place when the canvas turns to face the camera. Safe to run again: bars that are already the
// prefab are left alone.
public static class HealthBarSetup
{
    private const string PrefabFolder = "Assets/Prefabs/UI";
    private const string PrefabPath = PrefabFolder + "/HealthBar.prefab";
    private const string SpritePath = "Assets/Textures/UI/White_1x1.png";
    private static readonly Color OutlineColor = new Color32(0x35, 0x30, 0x33, 0xFF);
    private static readonly Color BackgroundColor = new Color(0.22f, 0.22f, 0.22f);
    private static readonly Color BarColor = new Color(0.92f, 0.14f, 0.14f);
    private static readonly Vector2 DefaultBarSize = new Vector2(0.77f, 0.14f);
    private const float OutlineThickness = 0.0625f; // one pixel of the 16-pixels-per-unit art

    [MenuItem("Tools/Set Up Health Bar Prefab")]
    private static void Run()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (prefab == null)
            prefab = CreatePrefab();

        int replaced = 0;
        foreach (string guid in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/Prefabs" }))
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            if (path == PrefabPath) continue;

            GameObject root = PrefabUtility.LoadPrefabContents(path);
            int count = 0;
            foreach (HealthBar bar in root.GetComponentsInChildren<HealthBar>(true))
                if (Replace(bar, prefab, false))
                    count++;
            if (count > 0)
                PrefabUtility.SaveAsPrefabAsset(root, path);
            PrefabUtility.UnloadPrefabContents(root);
            replaced += count;
        }

        foreach (HealthBar bar in Object.FindObjectsByType<HealthBar>(FindObjectsInactive.Include))
            if (Replace(bar, prefab, true))
                replaced++;

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log($"Health bars: replaced {replaced} with {PrefabPath}. Save the scene to keep the scene's ones.");
    }

    // Swaps one old health bar for the prefab. False if it's already the prefab, or it belongs to another
    // prefab instance (it's swapped in that prefab instead).
    private static bool Replace(HealthBar old, GameObject prefab, bool undoable)
    {
        GameObject oldObject = old.gameObject;
        if (PrefabUtility.IsPartOfPrefabInstance(oldObject)) return false;

        var serialized = new SerializedObject(old);
        Object owner = serialized.FindProperty("hasHealthGameobject").objectReferenceValue;
        var oldBar = serialized.FindProperty("barImage").objectReferenceValue as Image;
        Image oldBackground = oldObject.GetComponentsInChildren<Image>(true).FirstOrDefault(image => image != oldBar);

        // Where the bar itself was, which is where the new canvas is centered
        Transform oldTransform = oldObject.transform;
        Vector3 barOffset = oldBar != null ? oldBar.rectTransform.localPosition : Vector3.zero;
        Vector3 position = oldTransform.localPosition + oldTransform.localRotation * Vector3.Scale(oldTransform.localScale, barOffset);
        Vector2 barSize = oldBar != null ? oldBar.rectTransform.sizeDelta : DefaultBarSize;

        var instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, oldTransform.parent);
        if (undoable)
            Undo.RegisterCreatedObjectUndo(instance, "Replace health bar");
        instance.transform.SetSiblingIndex(oldTransform.GetSiblingIndex());
        instance.transform.localPosition = position;
        instance.transform.localRotation = oldTransform.localRotation;
        instance.transform.localScale = oldTransform.localScale;
        instance.SetActive(oldObject.activeSelf);

        var newBar = instance.GetComponent<HealthBar>();
        var newSerialized = new SerializedObject(newBar);
        newSerialized.FindProperty("hasHealthGameobject").objectReferenceValue = owner;
        newSerialized.ApplyModifiedPropertiesWithoutUndo();

        var frame = (RectTransform)instance.transform.Find("Frame");
        frame.sizeDelta = barSize + Vector2.one * OutlineThickness * 2f;
        if (oldBackground != null)
            frame.Find("Background").GetComponent<Image>().color = oldBackground.color;
        if (oldBar != null)
            frame.Find("Bar").GetComponent<Image>().color = oldBar.color;

        if (undoable)
            Undo.DestroyObjectImmediate(oldObject);
        else
            Object.DestroyImmediate(oldObject);
        return true;
    }

    // World-space canvas facing the camera: an outline frame with the dark background and the red fill inside
    private static GameObject CreatePrefab()
    {
        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets/Prefabs", "UI");
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(SpritePath);

        var root = new GameObject("HealthBar", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        root.GetComponent<Canvas>().renderMode = RenderMode.WorldSpace;
        ((RectTransform)root.transform).sizeDelta = new Vector2(1f, 0.3f);
        root.AddComponent<LookAtCamera>().mode = LookAtCamera.Mode.CameraForward;
        var healthBar = root.AddComponent<HealthBar>();

        Image frame = CreateImage("Frame", root.transform, sprite, OutlineColor);
        RectTransform frameRect = frame.rectTransform;
        frameRect.anchorMin = frameRect.anchorMax = new Vector2(0.5f, 0.5f);
        frameRect.sizeDelta = DefaultBarSize + Vector2.one * OutlineThickness * 2f;

        Image background = CreateImage("Background", frameRect, sprite, BackgroundColor);
        Inset(background.rectTransform);

        Image bar = CreateImage("Bar", frameRect, sprite, BarColor);
        Inset(bar.rectTransform);
        bar.type = Image.Type.Filled;
        bar.fillMethod = Image.FillMethod.Horizontal;
        bar.fillOrigin = (int)Image.OriginHorizontal.Left;

        var serialized = new SerializedObject(healthBar);
        serialized.FindProperty("barImage").objectReferenceValue = bar;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite, Color color)
    {
        var image = new GameObject(name, typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        image.rectTransform.SetParent(parent, false);
        image.sprite = sprite;
        image.color = color;
        image.raycastTarget = false;
        return image;
    }

    // Fills the frame, leaving the outline showing round the edge
    private static void Inset(RectTransform rect)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.one * OutlineThickness;
        rect.offsetMax = -Vector2.one * OutlineThickness;
    }
}
