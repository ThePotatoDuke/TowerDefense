using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Tools > Create Main Menu: makes Scenes/MainMenu.unity (a camera, a canvas with the wavy game title, Play
// and Quit buttons, a strip of the game's grass and flowers along the bottom, and an event system for
// mouse, keys and gamepads) and puts it first in the build, before the game scene, so the game starts
// there and the game-over screen's Main Menu button can go back to it. Run it with the game scene open:
// the grass and flowers are taken from its GroundDecorator. An existing menu scene is left alone (delete
// it to rebuild it), but the build order is still set.
public static class MainMenuSetup
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string Title = "KEEP OF THE GRASS";

    private static readonly Color Background = new(0.17f, 0.26f, 0.16f);
    private static readonly Color LightText = new(1f, 0.96f, 0.86f);

    [MenuItem("Tools/Create Main Menu")]
    private static void Create()
    {
        if (File.Exists(ScenePath))
        {
            Debug.Log($"Main menu: {ScenePath} already exists; delete it first to rebuild it.");
            PutFirstInBuild();
            return;
        }

        List<Sprite> decor = DecorFromGroundDecorator();
        TMP_FontAsset titleFont = UILayoutSetup.GetFontAsset(UILayoutSetup.TitleFontPath);
        TMP_FontAsset textFont = UILayoutSetup.GetFontAsset(UILayoutSetup.TextFontPath);
        Sprite board = UILayoutSetup.LoadSprite(UILayoutSetup.BoardPath);

        // Built in a new scene alongside the open one, which is left as it is
        Scene previous = SceneManager.GetActiveScene();
        Scene menuScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.SetActiveScene(menuScene);

        var cameraObject = new GameObject("Main Camera") { tag = "MainCamera" };
        var camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Background;

        var events = new GameObject("EventSystem", typeof(EventSystem));
        events.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

        var canvasObject = new GameObject("Canvas", typeof(RectTransform));
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        var scaler = canvasObject.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;
        canvasObject.AddComponent<GraphicRaycaster>();

        AddDecor(canvas, decor);

        TextMeshProUGUI title = UILayoutSetup.CreateText("Title", canvas.transform, Title, titleFont, 140f);
        title.color = LightText;
        title.enableAutoSizing = true; // shrinks to fit if the title font runs wide
        title.fontSizeMin = 60f;
        title.fontSizeMax = 140f;
        Place(title.rectTransform, new Vector2(0f, 200f), new Vector2(1600f, 220f));
        title.gameObject.AddComponent<WavyText>();

        var menu = canvasObject.AddComponent<MainMenu>();
        Button play = CreateButton("Play", canvas, board, textFont, new Vector2(0f, -60f));
        Button quit = CreateButton("Quit", canvas, board, textFont, new Vector2(0f, -170f));
        UnityEventTools.AddPersistentListener(play.onClick, menu.Play);
        UnityEventTools.AddPersistentListener(quit.onClick, menu.Quit);

        var serialized = new SerializedObject(menu);
        serialized.FindProperty("firstButton").objectReferenceValue = play;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(menuScene, ScenePath);
        SceneManager.SetActiveScene(previous);
        EditorSceneManager.CloseScene(menuScene, true);

        PutFirstInBuild();
        Debug.Log($"Main menu: made {ScenePath} and put it first in the build. Open it to see or edit it.");
    }

    // The menu first, so the game starts on it, then the game scene (and anything else already listed)
    private static void PutFirstInBuild()
    {
        List<EditorBuildSettingsScene> scenes = EditorBuildSettings.scenes.ToList();
        scenes.RemoveAll(scene => scene.path == ScenePath);
        scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
        EditorBuildSettings.scenes = scenes.ToArray();
    }

    // The grass tufts and flowers the game scene scatters on the ground
    private static List<Sprite> DecorFromGroundDecorator()
    {
        var sprites = new List<Sprite>();
        var decorator = Object.FindAnyObjectByType<GroundDecorator>(FindObjectsInactive.Include);
        if (decorator == null)
        {
            Debug.LogWarning("Main menu: no GroundDecorator in the open scene, so the menu has no grass strip.");
            return sprites;
        }

        var serialized = new SerializedObject(decorator);
        foreach (string list in new[] { "grassSprites", "flowerSprites" })
        {
            SerializedProperty property = serialized.FindProperty(list);
            for (int i = 0; property != null && i < property.arraySize; i++)
                if (property.GetArrayElementAtIndex(i).objectReferenceValue is Sprite sprite)
                    sprites.Add(sprite);
        }
        return sprites;
    }

    // A strip of grass and flowers along the bottom edge, at the UI pixel scale, always the same pattern
    private static void AddDecor(Canvas canvas, List<Sprite> decor)
    {
        if (decor.Count == 0) return;

        RectTransform strip = UILayoutSetup.CreateRect("Grass", canvas.transform);
        strip.anchorMin = new Vector2(0f, 0f);
        strip.anchorMax = new Vector2(1f, 0f);
        strip.pivot = new Vector2(0.5f, 0f);
        strip.sizeDelta = new Vector2(0f, 120f);
        strip.anchoredPosition = Vector2.zero;

        var random = new System.Random(7);
        const float width = 1920f;
        for (float x = 20f; x < width; x += 40f + (float)random.NextDouble() * 50f)
        {
            Sprite sprite = decor[random.Next(decor.Count)];
            Image image = UILayoutSetup.CreateImage(sprite.name, strip, sprite);
            UIPixels.Fit(image);
            RectTransform rect = image.rectTransform;
            rect.anchorMin = rect.anchorMax = new Vector2(x / width, 0f);
            rect.pivot = new Vector2(0.5f, 0f);
            // Whole art pixels up, so the strip stays crisp
            rect.anchoredPosition = new Vector2(0f, random.Next(0, 6) * UIPixels.Scale);
        }
    }

    private static Button CreateButton(string label, Canvas canvas, Sprite board, TMP_FontAsset font, Vector2 position)
    {
        Image image = UILayoutSetup.CreateImage(label, canvas.transform, board);
        image.raycastTarget = true;
        UIPixels.Slice(image, canvas);
        Place(image.rectTransform, position, new Vector2(70f, 22f) * UIPixels.Scale);

        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.93f, 0.7f);
        colors.selectedColor = new Color(1f, 0.93f, 0.7f);
        colors.pressedColor = new Color(0.85f, 0.8f, 0.65f);
        button.colors = colors;

        TextMeshProUGUI text = UILayoutSetup.CreateText("Label", image.transform, label, font, 40f);
        UILayoutSetup.Stretch(text.rectTransform, Vector2.zero, Vector2.zero);
        return button;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
