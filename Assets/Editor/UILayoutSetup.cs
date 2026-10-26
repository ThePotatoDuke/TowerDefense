using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// Tools > Create UI Layout Objects: builds the grass counter (on the existing UI canvas) and the game-over
// panel's contents as real objects in the open scene, so they can be moved and edited like any other UI.
// Tools > Create TextMeshPro Fonts (also run by the above): turns every font file in Assets/Fonts into a
// TextMeshPro font asset saved next to it, so TextMeshPro texts can use it.
// Safe to run again: parts that already exist are left alone.
public static class UILayoutSetup
{
    private const string BoardPath = "Assets/3rd Party/UI/currency_board.png";
    private const string GameOverBoardPath = "Assets/3rd Party/UI/game_over_board.png";
    private const string GrassSheetPath = "Assets/3rd Party/Sprites/Environment/GRASS+.png";
    private const string GrassIconName = "GRASS+_265";
    private const string FontsFolder = "Assets/Fonts";
    private const string TitleFontPath = "Assets/Fonts/OrigamiMommy.ttf";
    private const string TextFontPath = "Assets/Fonts/PerfectDOSVGA437.ttf";

    private static readonly Color TextColor = new Color(0.23f, 0.16f, 0.15f);

    [MenuItem("Tools/Create UI Layout Objects")]
    private static void Create()
    {
        CreateAllFontAssets();
        TMP_FontAsset titleFont = GetFontAsset(TitleFontPath);
        TMP_FontAsset textFont = GetFontAsset(TextFontPath);

        var currency = Object.FindAnyObjectByType<CurrencyUI>(FindObjectsInactive.Include);
        if (currency != null)
            CreateGrassCounter(currency, textFont);
        else
            Debug.LogWarning("UI layout: no CurrencyUI in the open scene, so no grass counter was made.");

        var gameOver = Object.FindAnyObjectByType<GameOverScreen>(FindObjectsInactive.Include);
        if (gameOver != null)
            CreateGameOver(gameOver, titleFont, textFont);
        else
            Debug.LogWarning("UI layout: no GameOverScreen in the open scene, so the game-over panel was left alone.");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("UI layout objects created. Save the scene to keep them.");
    }

    // Board with the grass icon on its left and the count filling the rest, in the top-left corner of the
    // canvas the CurrencyUI is on (straight under it, not inside whatever panel holds the script)
    private static void CreateGrassCounter(CurrencyUI currency, TMP_FontAsset font)
    {
        Canvas canvas = currency.GetComponentInParent<Canvas>(true);
        Transform parent = canvas != null ? canvas.rootCanvas.transform : currency.transform;
        if (parent.Find("GrassCounter") != null)
        {
            Debug.Log("UI layout: GrassCounter already exists; delete it first to rebuild it.");
            return;
        }

        const float pixelScale = 6f;  // screen units per board pixel
        const float inset = 5f * pixelScale;
        const float iconSize = 60f;

        Sprite board = LoadSprite(BoardPath);
        Sprite icon = AssetDatabase.LoadAllAssetsAtPath(GrassSheetPath).OfType<Sprite>().FirstOrDefault(s => s.name == GrassIconName);

        RectTransform counter = CreateRect("GrassCounter", parent);
        counter.anchorMin = counter.anchorMax = counter.pivot = new Vector2(0f, 1f);
        counter.anchoredPosition = new Vector2(24f, -24f);
        counter.sizeDelta = board != null ? board.rect.size * pixelScale : new Vector2(192f, 96f);

        Image boardImage = CreateImage("Board", counter, board);
        Stretch(boardImage.rectTransform, Vector2.zero, Vector2.zero);

        Image iconImage = CreateImage("Icon", counter, icon);
        RectTransform iconRect = iconImage.rectTransform;
        iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(inset, 0f);
        iconRect.sizeDelta = Vector2.one * iconSize;
        iconImage.preserveAspect = true;

        TextMeshProUGUI count = CreateText("Count", counter, "0", font, 48f);
        Stretch(count.rectTransform, new Vector2(inset + iconSize + 8f, 0f), new Vector2(-inset, 0f));

        var serialized = new SerializedObject(currency);
        serialized.FindProperty("counter").objectReferenceValue = counter;
        serialized.FindProperty("countText").objectReferenceValue = count;
        serialized.ApplyModifiedProperties();
    }

    // Board behind the panel, the wavy title, the wave line, and the buttons restyled to match
    private static void CreateGameOver(GameOverScreen gameOver, TMP_FontAsset titleFont, TMP_FontAsset textFont)
    {
        const float pixelScale = 4f;
        var panel = (RectTransform)gameOver.transform;

        Sprite board = LoadSprite(GameOverBoardPath);
        Sprite buttonBoard = LoadSprite(BoardPath);

        var panelImage = panel.GetComponent<Image>();
        if (panelImage != null && board != null && panelImage.sprite != board)
        {
            Undo.RecordObject(panelImage, "Game over board");
            panelImage.sprite = board;
            panelImage.type = Image.Type.Simple;
            panelImage.color = Color.white;
            Undo.RecordObject(panel, "Game over board");
            panel.sizeDelta = board.rect.size * pixelScale;
        }

        if (panel.Find("Title") == null)
        {
            TextMeshProUGUI title = CreateText("Title", panel, "GAME OVER", titleFont, 80f);
            PlaceCentered(title.rectTransform, panel, 170f, 120f);
            Undo.AddComponent<WavyText>(title.gameObject);
        }

        if (panel.Find("WaveReached") == null)
        {
            TextMeshProUGUI wave = CreateText("WaveReached", panel, "Wave reached: 0", textFont, 44f);
            PlaceCentered(wave.rectTransform, panel, 30f, 66f);

            var serialized = new SerializedObject(gameOver);
            serialized.FindProperty("waveText").objectReferenceValue = wave;
            serialized.ApplyModifiedProperties();
        }

        foreach (Button button in panel.GetComponentsInChildren<Button>(true))
        {
            var rect = (RectTransform)button.transform;
            Undo.RecordObject(rect, "Style button");
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(rect.anchoredPosition.x, -140f);

            if (buttonBoard != null && button.targetGraphic is Image buttonImage)
            {
                Undo.RecordObject(buttonImage, "Style button");
                buttonImage.sprite = buttonBoard;
                buttonImage.type = Image.Type.Simple;
                rect.sizeDelta = buttonBoard.rect.size * pixelScale;
            }

            foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
            {
                Undo.RecordObject(label, "Style button");
                if (textFont != null)
                    label.font = textFont;
                label.text = label.text.Trim();
                label.fontSize = 40f;
                label.color = TextColor;
            }
        }
    }

    [MenuItem("Tools/Create TextMeshPro Fonts")]
    private static void CreateAllFontAssets()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Font", new[] { FontsFolder }))
            GetFontAsset(AssetDatabase.GUIDToAssetPath(guid));
    }

    // A TextMeshPro font asset for the font file, made once and saved beside it as "<name> SDF.asset"
    private static TMP_FontAsset GetFontAsset(string fontPath)
    {
        string assetPath = FontAssetPath(fontPath);
        var existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
        if (existing != null) return existing;
        Debug.Log($"UI layout: making TextMeshPro font {assetPath}");

        var font = AssetDatabase.LoadAssetAtPath<Font>(fontPath);
        if (font == null)
        {
            Debug.LogWarning($"UI layout: font file not found at {fontPath}");
            return null;
        }
        return CreateAndSave(font, assetPath, Path.GetFileNameWithoutExtension(assetPath));
    }

    private static TMP_FontAsset CreateAndSave(Font font, string assetPath, string name)
    {
        // Dynamic: characters are added to the atlas as they're used
        TMP_FontAsset asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024,
                                                            AtlasPopulationMode.Dynamic);
        if (asset == null)
        {
            Debug.LogWarning($"UI layout: TextMeshPro couldn't read the font {font.name}");
            return null;
        }
        asset.name = name;

        AssetDatabase.CreateAsset(asset, assetPath);

        // The atlas and material live inside the font asset
        asset.atlasTextures[0].name = asset.name + " Atlas";
        asset.material.name = asset.name + " Material";
        AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
        AssetDatabase.AddObjectToAsset(asset.material, asset);
        AssetDatabase.SaveAssets();
        return asset;
    }

    private static string FontAssetPath(string fontPath) =>
        SiblingPath(fontPath, Path.GetFileNameWithoutExtension(fontPath) + " SDF.asset");

    private static string SiblingPath(string path, string fileName) =>
        Path.Combine(Path.GetDirectoryName(path), fileName).Replace(Path.DirectorySeparatorChar, '/');

    private static Sprite LoadSprite(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning($"UI layout: no sprite at {path} (is it imported as a Sprite?)");
        return sprite;
    }

    private static RectTransform CreateRect(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(gameObject, "Create " + name);
        gameObject.layer = parent.gameObject.layer;
        var rect = (RectTransform)gameObject.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    private static Image CreateImage(string name, Transform parent, Sprite sprite)
    {
        var image = CreateRect(name, parent).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    private static TextMeshProUGUI CreateText(string name, Transform parent, string content, TMP_FontAsset font, float size)
    {
        var text = CreateRect(name, parent).gameObject.AddComponent<TextMeshProUGUI>();
        if (font != null)
            text.font = font;
        text.text = content;
        text.fontSize = size;
        text.color = TextColor;
        text.alignment = TextAlignmentOptions.Center;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.raycastTarget = false;
        return text;
    }

    private static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void PlaceCentered(RectTransform rect, RectTransform panel, float y, float height)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(panel.sizeDelta.x, height);
    }
}
