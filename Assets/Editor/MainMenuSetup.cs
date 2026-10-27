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

// Tools > Create Main Menu: makes Scenes/MainMenu.unity (a camera, a canvas with a drifting diamond
// background, the title sign, Play and Quit buttons, a strip of the game's grass and flowers along the bottom, and an event system for
// mouse, keys and gamepads) and puts it first in the build, before the game scene, so the game starts
// there and the game-over screen's Main Menu button can go back to it. Run it with the game scene open:
// the grass and flowers are taken from its GroundDecorator. An existing menu scene is left alone (delete
// it to rebuild it), but the build order is still set. Tools > Add Main Menu Background And Sign adds the
// background and sign to the open menu scene instead (hiding the old text title), and Tools > Set Up Main
// Menu Intro lays out the opening (MenuIntro): the sign at the top, the buttons hanging below it on chains,
// all at the sign's pixel size.
public static class MainMenuSetup
{
    private const string ScenePath = "Assets/Scenes/MainMenu.unity";
    private const string Art = "Assets/3rd Party/Sprites/Environment/";
    private const string SignPath = Art + "title_sign.png";
    private const string CleanSignPath = Art + "title_sign_clean.png";
    private const string FlashPath = Art + "title_sign_flash.png"; // made from the clean sign
    private const string ChainPath = Art + "chain_link.png";

    // The menu's pixel art is bigger than the other UI's but still a whole number of units per art pixel, so
    // it stays crisp; every size and position below is in whole art pixels of it
    private const float MenuScale = 5f;
    private const float SignTop = 106f * MenuScale;          // the sign's top edge, up from the middle
    private static readonly Vector2 ButtonPixels = new(70f, 22f);
    private const float ChainGap = 10f * MenuScale;           // between the sign and a button, and between buttons
    private const float ChainOverlap = 3f * MenuScale;        // how far each chain end tucks behind what it holds
    private const float ChainInset = 12f * MenuScale;         // from a button's side to its chains
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

        AddBackground(canvas);
        AddDecor(canvas, decor);

        TextMeshProUGUI title = UILayoutSetup.CreateText("Title", canvas.transform, Title, titleFont, 140f);
        title.color = LightText;
        title.enableAutoSizing = true; // shrinks to fit if the title font runs wide
        title.fontSizeMin = 60f;
        title.fontSizeMax = 140f;
        Place(title.rectTransform, new Vector2(0f, 200f), new Vector2(1600f, 220f));
        title.gameObject.AddComponent<WavyText>();

        var menu = canvasObject.AddComponent<MainMenu>();
        Button play = CreateButton("Play", canvas, board, textFont, Vector2.zero);
        Button quit = CreateButton("Quit", canvas, board, textFont, Vector2.zero);
        UnityEventTools.AddPersistentListener(play.onClick, menu.Play);
        UnityEventTools.AddPersistentListener(quit.onClick, menu.Quit);

        if (AddSign(canvas, title))
            SetUpIntro(canvas); // lays out the sign and buttons

        var serialized = new SerializedObject(menu);
        serialized.FindProperty("firstButton").objectReferenceValue = play;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.SaveScene(menuScene, ScenePath);
        SceneManager.SetActiveScene(previous);
        EditorSceneManager.CloseScene(menuScene, true);

        PutFirstInBuild();
        Debug.Log($"Main menu: made {ScenePath} and put it first in the build. Open it to see or edit it.");
    }

    [MenuItem("Tools/Add Main Menu Background And Sign")]
    private static void AddToOpenMenu()
    {
        var menu = Object.FindAnyObjectByType<MainMenu>(FindObjectsInactive.Include);
        Canvas canvas = menu != null ? menu.GetComponentInParent<Canvas>() : null;
        if (canvas == null)
        {
            Debug.LogWarning("Main menu: open the MainMenu scene first (no MainMenu canvas found).");
            return;
        }

        AddBackground(canvas);
        Transform title = canvas.transform.Find("Title");
        Transform existing = canvas.transform.Find("Sign");
        if (existing == null)
            AddSign(canvas, title != null ? title.GetComponent<TMP_Text>() : null);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("Main menu: added (or resized) the diamond background and title sign. Save the scene to keep them.");
    }

    // The diamonds filling the screen behind everything, drifting (their size and colours are set on it)
    private static void AddBackground(Canvas canvas)
    {
        if (canvas.transform.Find("Background") != null) return;

        RectTransform rect = UILayoutSetup.CreateRect("Background", canvas.transform);
        rect.SetAsFirstSibling();
        UILayoutSetup.Stretch(rect, Vector2.zero, Vector2.zero);
        var image = rect.gameObject.AddComponent<RawImage>();
        image.raycastTarget = false;
        rect.gameObject.AddComponent<ScrollingBackground>();
    }

    // The title sign at the UI pixel scale, in place of the text title (hidden, not deleted, so its look is kept)
    private static bool AddSign(Canvas canvas, TMP_Text title)
    {
        Sprite sign = PixelSprites.Import(SignPath);
        if (sign == null)
        {
            Debug.LogWarning($"Main menu: no sprite at {SignPath}, so the text title stays.");
            return false;
        }

        Image image = UILayoutSetup.CreateImage("Sign", canvas.transform, sign);
        PlaceSign(image);
        if (title != null)
        {
            image.transform.SetSiblingIndex(title.transform.GetSiblingIndex());
            Undo.RecordObject(title.gameObject, "Hide Title");
            title.gameObject.SetActive(false);
        }
        return true;
    }

    // Hung from its top edge at the top of the screen
    private static bool PlaceSign(Image image)
    {
        if (image == null || image.sprite == null) return false;
        Undo.RecordObject(image.rectTransform, "Place Sign");
        PlaceFromTop(image.rectTransform, SignTop, image.sprite.rect.size * MenuScale);
        return true;
    }

    [MenuItem("Tools/Set Up Main Menu Intro")]
    private static void SetUpOpenMenuIntro()
    {
        var menu = Object.FindAnyObjectByType<MainMenu>(FindObjectsInactive.Include);
        Canvas canvas = menu != null ? menu.GetComponentInParent<Canvas>() : null;
        if (canvas == null)
        {
            Debug.LogWarning("Main menu: open the MainMenu scene first (no MainMenu canvas found).");
            return;
        }
        if (canvas.transform.Find("Sign") == null)
        {
            Debug.LogWarning("Main menu: no Sign yet; run Tools > Add Main Menu Background And Sign first.");
            return;
        }

        SetUpIntro(canvas);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("Main menu: intro set up (timings are on the Canvas's MenuIntro). Save the scene to keep it.");
    }

    // The sign at the top, Play hanging from it on two chains and Quit from Play, everything at the menu's
    // pixel size, ordered so each button is behind the one above (it drops out from behind it), and the
    // MenuIntro that plays it all
    private static void SetUpIntro(Canvas canvas)
    {
        Sprite dirty = PixelSprites.Import(SignPath);
        Sprite clean = PixelSprites.Import(CleanSignPath);
        Sprite chain = PixelSprites.Import(ChainPath, fullRect: true);
        Sprite flashSprite = MakeFlash(CleanSignPath, FlashPath);

        Transform root = canvas.transform;
        var sign = root.Find("Sign").GetComponent<Image>();
        Undo.RecordObject(sign, "Set Up Intro");
        sign.sprite = dirty; // the finished look while editing; the intro starts it clean
        PlaceSign(sign);

        Image flash = sign.transform.Find("Flash")?.GetComponent<Image>();
        if (flash == null)
        {
            flash = UILayoutSetup.CreateImage("Flash", sign.transform, flashSprite);
            UILayoutSetup.Stretch(flash.rectTransform, Vector2.zero, Vector2.zero);
        }
        flash.sprite = flashSprite;
        flash.color = new Color(1f, 1f, 1f, 0f);

        var buttons = new List<Button>();
        float top = SignTop - sign.rectTransform.sizeDelta.y - ChainGap;
        foreach (string name in new[] { "Play", "Quit" })
        {
            Button button = root.Find(name)?.GetComponent<Button>();
            if (button == null) continue;
            var image = (Image)button.targetGraphic;
            Undo.RecordObject(image, "Set Up Intro");
            SliceAt(image, canvas, MenuScale);
            var rect = (RectTransform)button.transform;
            Undo.RecordObject(rect, "Set Up Intro");
            PlaceFromTop(rect, top, ButtonPixels * MenuScale);
            if (chain != null)
                AddChains(rect, chain, canvas);
            buttons.Add(button);
            top -= rect.sizeDelta.y + ChainGap;
        }

        // Back to front: the lowest button first, so each hides behind the one above and the sign
        for (int i = buttons.Count - 1; i >= 0; i--)
            buttons[i].transform.SetAsLastSibling();
        sign.transform.SetAsLastSibling();

        var intro = canvas.GetComponent<MenuIntro>();
        if (intro == null)
            intro = Undo.AddComponent<MenuIntro>(canvas.gameObject);
        var serialized = new SerializedObject(intro);
        serialized.FindProperty("sign").objectReferenceValue = sign;
        serialized.FindProperty("flash").objectReferenceValue = flash;
        serialized.FindProperty("cleanSign").objectReferenceValue = clean;
        serialized.FindProperty("dirtySign").objectReferenceValue = dirty;
        SerializedProperty list = serialized.FindProperty("buttons");
        list.arraySize = buttons.Count;
        for (int i = 0; i < buttons.Count; i++)
            list.GetArrayElementAtIndex(i).objectReferenceValue = buttons[i];
        serialized.ApplyModifiedProperties();
    }

    // Two chains up from the button's top edge to whatever's above, tucked behind it at both ends
    private static void AddChains(RectTransform button, Sprite link, Canvas canvas)
    {
        float halfWidth = button.sizeDelta.x * 0.5f;
        foreach ((string name, float x) in new[] { ("Chain L", -halfWidth + ChainInset), ("Chain R", halfWidth - ChainInset) })
        {
            Image chain = button.Find(name)?.GetComponent<Image>();
            if (chain == null)
            {
                chain = UILayoutSetup.CreateImage(name, button, link);
                chain.transform.SetAsFirstSibling(); // under the label
            }
            Undo.RecordObject(chain, "Set Up Intro");
            chain.sprite = link;
            chain.type = Image.Type.Tiled;
            chain.pixelsPerUnitMultiplier = canvas.rootCanvas.referencePixelsPerUnit / (link.pixelsPerUnit * MenuScale);
            RectTransform rect = chain.rectTransform;
            Undo.RecordObject(rect, "Set Up Intro");
            rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 0f);
            rect.anchoredPosition = new Vector2(x, -ChainOverlap);
            rect.sizeDelta = new Vector2(link.rect.width * MenuScale, ChainGap + ChainOverlap * 2f);
        }
    }

    // A white silhouette of the sign, for the flash (remade each time, so it follows the sign art)
    private static Sprite MakeFlash(string sourcePath, string flashPath)
    {
        if (!File.Exists(sourcePath)) return null;

        var texture = new Texture2D(2, 2);
        texture.LoadImage(File.ReadAllBytes(sourcePath));
        Color32[] pixels = texture.GetPixels32();
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = new Color32(255, 255, 255, pixels[i].a);
        texture.SetPixels32(pixels);
        File.WriteAllBytes(flashPath, texture.EncodeToPNG());
        Object.DestroyImmediate(texture);

        AssetDatabase.ImportAsset(flashPath);
        return PixelSprites.Import(flashPath);
    }

    // A sliced board drawn at the given units per art pixel
    private static void SliceAt(Image image, Canvas canvas, float scale)
    {
        if (image.sprite == null) return;
        image.type = Image.Type.Sliced;
        image.pixelsPerUnitMultiplier = canvas.rootCanvas.referencePixelsPerUnit / (image.sprite.pixelsPerUnit * scale);
    }

    // Centred across, its top edge this far up from the middle of the screen
    private static void PlaceFromTop(RectTransform rect, float top, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, top);
        rect.sizeDelta = size;
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
