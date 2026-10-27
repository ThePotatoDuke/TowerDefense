using System.Collections.Generic;
using System.IO;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

// Tools > Create UI Layout Objects: builds the grass counter and the weapon bar (on the existing UI canvas)
// and lays out the game-over panel (board, title, wave line, Retry and Main Menu buttons) as real objects
// in the open scene, so they can be moved and edited like any other UI. All pixel art is drawn at the one
// UI pixel scale (UIPixels). Safe to run again: the counter and bar are left alone if they exist, the
// game-over panel is laid out afresh.
// Tools > Rebuild All UI: deletes the grass counter, weapon bar, shop window and held-item icons and builds
// them all again (with Tools > Create Shop), e.g. after the layout code changes.
// Tools > Create TextMeshPro Fonts (also run by the above): turns every font file in Assets/Fonts into a
// TextMeshPro font asset saved next to it, so TextMeshPro texts can use it.
public static class UILayoutSetup
{
    internal const string BoardPath = "Assets/3rd Party/UI/currency_board.png";
    private const string GameOverBoardPath = "Assets/3rd Party/UI/game_over_board.png";
    internal const string GrassSheetPath = "Assets/3rd Party/Sprites/Environment/GRASS+.png";
    internal const string GrassIconName = "GRASS+_265";
    private const string FontsFolder = "Assets/Fonts";
    internal const string TitleFontPath = "Assets/Fonts/OrigamiMommy.ttf";
    internal const string TextFontPath = "Assets/Fonts/PerfectDOSVGA437.ttf";

    internal static readonly Color TextColor = new Color(0.23f, 0.16f, 0.15f);

    private static readonly string[] BuiltUI = { "GrassCounter", "WeaponBar", "Shop", "HeldItems" };

    // Keeps how the texts were set up: each rebuilt text gets back the font, font material, size and
    // colour of the text that was at the same place in the old one
    [MenuItem("Tools/Rebuild All UI")]
    private static void RebuildAll()
    {
        var currency = Object.FindAnyObjectByType<CurrencyUI>(FindObjectsInactive.Include);
        Canvas canvas = currency != null ? currency.GetComponentInParent<Canvas>(true) : null;
        Transform canvasRoot = canvas != null ? canvas.rootCanvas.transform : null;
        var looks = new Dictionary<string, (TMP_FontAsset font, Material material, float size, bool autoSize,
                                            float sizeMin, float sizeMax, Color color)>();
        if (canvasRoot != null)
        {
            foreach (string name in BuiltUI)
            {
                Transform old = canvasRoot.Find(name);
                if (old == null) continue;
                foreach (TMP_Text text in old.GetComponentsInChildren<TMP_Text>(true))
                    looks[PathUnder(text.transform, canvasRoot)] = (text.font, text.fontSharedMaterial, text.fontSize,
                        text.enableAutoSizing, text.fontSizeMin, text.fontSizeMax, text.color);
                Undo.DestroyObjectImmediate(old.gameObject);
            }
        }

        Create();
        EditorApplication.ExecuteMenuItem("Tools/Create Shop");

        if (canvasRoot == null) return;
        foreach (string name in BuiltUI)
        {
            Transform rebuilt = canvasRoot.Find(name);
            if (rebuilt == null) continue;
            foreach (TMP_Text text in rebuilt.GetComponentsInChildren<TMP_Text>(true))
            {
                if (!looks.TryGetValue(PathUnder(text.transform, canvasRoot), out var kept)) continue;
                if (kept.font != null)
                    text.font = kept.font;
                if (kept.material != null)
                    text.fontSharedMaterial = kept.material;
                text.fontSize = kept.size;
                text.enableAutoSizing = kept.autoSize;
                text.fontSizeMin = kept.sizeMin;
                text.fontSizeMax = kept.sizeMax;
                text.color = kept.color;
            }
        }
    }

    private static string PathUnder(Transform t, Transform root) =>
        t == root || t.parent == null ? "" : PathUnder(t.parent, root) + "/" + t.name;

    [MenuItem("Tools/Create UI Layout Objects")]
    private static void Create()
    {
        CreateAllFontAssets();
        TMP_FontAsset titleFont = GetFontAsset(TitleFontPath);
        TMP_FontAsset textFont = GetFontAsset(TextFontPath);

        var currency = Object.FindAnyObjectByType<CurrencyUI>(FindObjectsInactive.Include);
        if (currency != null)
        {
            CreateGrassCounter(currency, textFont);
            CreateWeaponBar(currency, textFont);
        }
        else
            Debug.LogWarning("UI layout: no CurrencyUI in the open scene, so no grass counter or weapon bar was made.");

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

        const float inset = 5f * UIPixels.Scale;

        Sprite board = LoadSprite(BoardPath);
        Sprite icon = AssetDatabase.LoadAllAssetsAtPath(GrassSheetPath).OfType<Sprite>().FirstOrDefault(s => s.name == GrassIconName);

        RectTransform counter = CreateRect("GrassCounter", parent);
        counter.anchorMin = counter.anchorMax = counter.pivot = new Vector2(0f, 1f);
        counter.anchoredPosition = new Vector2(24f, -24f);
        counter.sizeDelta = new Vector2(40f, 18f) * UIPixels.Scale; // wide enough for four digits

        Image boardImage = CreateImage("Board", counter, board);
        UIPixels.Slice(boardImage, canvas);
        Stretch(boardImage.rectTransform, Vector2.zero, Vector2.zero);

        Image iconImage = CreateImage("Icon", counter, icon);
        UIPixels.Fit(iconImage);
        RectTransform iconRect = iconImage.rectTransform;
        iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = new Vector2(inset, 0f);

        TextMeshProUGUI count = CreateText("Count", counter, "0", font, 40f);
        count.alignment = TextAlignmentOptions.Left;
        Stretch(count.rectTransform, new Vector2(inset + iconRect.sizeDelta.x + 8f, 0f), new Vector2(-inset, 0f));

        var serialized = new SerializedObject(currency);
        serialized.FindProperty("counter").objectReferenceValue = counter;
        serialized.FindProperty("countText").objectReferenceValue = count;
        serialized.ApplyModifiedProperties();
    }

    // A row of slots along the bottom middle of the same canvas: a board frame each, the weapon's 16x16
    // icon on top at its pixel size (set by WeaponBarUI) and the slot's number key in the corner
    private static void CreateWeaponBar(CurrencyUI currency, TMP_FontAsset font)
    {
        Canvas canvas = currency.GetComponentInParent<Canvas>(true);
        Transform parent = canvas != null ? canvas.rootCanvas.transform : currency.transform;
        if (parent.Find("WeaponBar") != null)
        {
            Debug.Log("UI layout: WeaponBar already exists; delete it first to rebuild it.");
            return;
        }

        const int slotCount = 3;
        var slotSize = new Vector2(24f, 24f) * UIPixels.Scale;
        const float spacing = 4f * UIPixels.Scale;

        Sprite board = LoadSprite(BoardPath);

        RectTransform bar = CreateRect("WeaponBar", parent);
        bar.anchorMin = bar.anchorMax = bar.pivot = new Vector2(0.5f, 0f);
        bar.anchoredPosition = new Vector2(0f, 24f);
        bar.sizeDelta = new Vector2(slotCount * slotSize.x + (slotCount - 1) * spacing, slotSize.y);

        var barUI = Undo.AddComponent<WeaponBarUI>(bar.gameObject);
        var serialized = new SerializedObject(barUI);
        SerializedProperty slots = serialized.FindProperty("slots");
        slots.arraySize = slotCount;

        for (int i = 0; i < slotCount; i++)
        {
            RectTransform slot = CreateRect("Slot " + (i + 1), bar);
            slot.anchorMin = slot.anchorMax = new Vector2(0f, 0.5f);
            slot.pivot = new Vector2(0.5f, 0.5f);
            slot.anchoredPosition = new Vector2(slotSize.x * 0.5f + i * (slotSize.x + spacing), 0f);
            slot.sizeDelta = slotSize;

            Image frame = CreateImage("Frame", slot, board);
            UIPixels.Slice(frame, canvas);
            Stretch(frame.rectTransform, Vector2.zero, Vector2.zero);

            Image icon = CreateImage("Icon", slot, null);
            icon.rectTransform.anchorMin = icon.rectTransform.anchorMax = icon.rectTransform.pivot = new Vector2(0.5f, 0.5f);
            icon.enabled = false;

            TextMeshProUGUI key = CreateText("Key", slot, (i + 1).ToString(), font, 24f);
            key.rectTransform.anchorMin = key.rectTransform.anchorMax = key.rectTransform.pivot = new Vector2(0f, 1f);
            key.rectTransform.anchoredPosition = new Vector2(10f, -4f);
            key.rectTransform.sizeDelta = new Vector2(28f, 28f);

            SerializedProperty view = slots.GetArrayElementAtIndex(i);
            view.FindPropertyRelative("root").objectReferenceValue = slot;
            view.FindPropertyRelative("frame").objectReferenceValue = frame;
            view.FindPropertyRelative("icon").objectReferenceValue = icon;
        }
        serialized.ApplyModifiedProperties();
    }

    // The board at the UI pixel scale, the wavy title and the wave line near the top, and the buttons
    // stacked below them: Retry, then Main Menu (wired to GameOverUI), restyled as sliced boards
    private static void CreateGameOver(GameOverScreen gameOver, TMP_FontAsset titleFont, TMP_FontAsset textFont)
    {
        var panel = (RectTransform)gameOver.transform;
        Canvas canvas = panel.GetComponentInParent<Canvas>(true);

        Sprite board = LoadSprite(GameOverBoardPath);
        Sprite buttonBoard = LoadSprite(BoardPath);

        var panelImage = panel.GetComponent<Image>();
        if (panelImage != null && board != null)
        {
            Undo.RecordObject(panelImage, "Game over board");
            panelImage.sprite = board;
            panelImage.type = Image.Type.Simple;
            panelImage.color = Color.white;
            Undo.RecordObject(panel, "Game over board");
            panel.sizeDelta = board.rect.size * UIPixels.Scale;
        }

        Transform titleTransform = panel.Find("Title");
        TMP_Text title = titleTransform != null ? titleTransform.GetComponent<TMP_Text>() : null;
        if (title == null)
        {
            title = CreateText("Title", panel, "GAME OVER", titleFont, 80f);
            Undo.AddComponent<WavyText>(title.gameObject);
        }
        Undo.RecordObject(title.rectTransform, "Game over layout");
        PlaceCentered(title.rectTransform, panel, 185f, 110f);

        Transform waveTransform = panel.Find("WaveReached");
        TMP_Text wave = waveTransform != null ? waveTransform.GetComponent<TMP_Text>() : null;
        if (wave == null)
        {
            wave = CreateText("WaveReached", panel, "Wave reached: 0", textFont, 40f);
            var serialized = new SerializedObject(gameOver);
            serialized.FindProperty("waveText").objectReferenceValue = wave;
            serialized.ApplyModifiedProperties();
        }
        Undo.RecordObject(wave.rectTransform, "Game over layout");
        PlaceCentered(wave.rectTransform, panel, 95f, 60f);

        var gameOverUI = panel.GetComponent<GameOverUI>();
        Button[] buttons = panel.GetComponentsInChildren<Button>(true);
        string[] labels = { "Retry", "Main Menu" };
        float[] heights = { -95f, -185f };
        for (int i = 0; i < buttons.Length; i++)
        {
            Button button = buttons[i];
            var rect = (RectTransform)button.transform;
            Undo.RecordObject(rect, "Game over layout");
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(0f, heights[Mathf.Min(i, heights.Length - 1)] - 90f * Mathf.Max(0, i - 1));
            rect.sizeDelta = new Vector2(56f, 18f) * UIPixels.Scale;

            if (buttonBoard != null && button.targetGraphic is Image buttonImage)
            {
                Undo.RecordObject(buttonImage, "Game over layout");
                buttonImage.sprite = buttonBoard;
                UIPixels.Slice(buttonImage, canvas);
            }

            foreach (TMP_Text label in button.GetComponentsInChildren<TMP_Text>(true))
            {
                Undo.RecordObject(label, "Game over layout");
                if (i < labels.Length)
                    label.text = labels[i];
                label.margin = new Vector4(28f, 0f, 28f, 0f); // clear of the board's edges
            }

            // The first retries, the second goes to the main menu
            if (gameOverUI != null && i < labels.Length)
            {
                Undo.RecordObject(button, "Game over layout");
                for (int listener = button.onClick.GetPersistentEventCount() - 1; listener >= 0; listener--)
                    UnityEventTools.RemovePersistentListener(button.onClick, listener);
                UnityAction action = i == 0 ? gameOverUI.Retry : (UnityAction)gameOverUI.QuitToMenu;
                UnityEventTools.AddPersistentListener(button.onClick, action);
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
    internal static TMP_FontAsset GetFontAsset(string fontPath)
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

    internal static Sprite LoadSprite(string path)
    {
        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning($"UI layout: no sprite at {path} (is it imported as a Sprite?)");
        return sprite;
    }

    internal static RectTransform CreateRect(string name, Transform parent)
    {
        var gameObject = new GameObject(name, typeof(RectTransform));
        Undo.RegisterCreatedObjectUndo(gameObject, "Create " + name);
        gameObject.layer = parent.gameObject.layer;
        var rect = (RectTransform)gameObject.transform;
        rect.SetParent(parent, false);
        return rect;
    }

    internal static Image CreateImage(string name, Transform parent, Sprite sprite)
    {
        var image = CreateRect(name, parent).gameObject.AddComponent<Image>();
        image.sprite = sprite;
        image.raycastTarget = false;
        return image;
    }

    internal static TextMeshProUGUI CreateText(string name, Transform parent, string content, TMP_FontAsset font, float size)
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

    internal static void Stretch(RectTransform rect, Vector2 offsetMin, Vector2 offsetMax)
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
