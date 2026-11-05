using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tools > Tidy HUD: lays out the open scene's HUD in one style. Every corner panel is the same wood board,
// the same height, at the HUD's 6x pixel size, with a 12x12 icon sitting on its left end and the same
// distance from the screen's edges:
//   top left:  the grass counter, sized for four digits
//   top right: the player's and the castle's health, one board each with a bar and "18/20" over it
// All their numbers (and the weapon slots' keys) are white with the dark pixel outline "Wave 1" uses, at
// one size. It also switches off the health bars floating over the player and the castle (enemies keep
// theirs) and moves the held seed and Totem icons down below the health. Running it again rebuilds the
// grass counter's insides and HealthBars from scratch (undoable), so hand-made changes to them are lost;
// the grass icon picked in the scene is kept.
public static class HudSetup
{
    private const string PlayerIconPath = "Assets/3rd Party/UI/Health_bar/player_health_bar.png";
    private const string CastleIconPath = "Assets/3rd Party/UI/Health_bar/tower_health_bar.png";
    private const string OutlineFontPath = "Assets/Fonts/BULKYPIX_outline.asset";
    private const string OutlineMaterialPath = "Assets/Fonts/BULKYPIX_outline Pixel Outline.mat";

    // Canvas units per art pixel: the same as the HUD's other boards (Pixels Per Unit Multiplier 1.0417)
    private const float PixelSize = 6f;
    private const float Margin = 24f;    // from the screen's edges, in canvas units
    private const float TextSize = 36f;  // every HUD number but the wave's

    // In art pixels. The board art (currency_board) has 3 clear pixels at each side and 2 at the top and
    // bottom, so a 16-high board shows 12 of wood and frame: the same as the icons.
    private const float BoardHeight = 16f;
    private const float GrassBoardWidth = 42f;  // four digits
    private const float HealthBoardWidth = 50f; // "20/20"
    private const float IconLeft = 3f;          // on the board's left edge, covering its end
    private const float IconSize = 12f;
    private const float ContentLeft = 16f;      // just past the icon
    private const float ContentRight = 4f;      // the frame on the right
    private const float RowGap = -2f;           // rows overlap their clear edges, so 2 pixels show between

    private static readonly Color PlayerColor = new(0.36f, 0.78f, 0.36f);
    private static readonly Color CastleColor = new(0.35f, 0.62f, 0.9f);
    private static readonly Color TrailColor = new(1f, 0.95f, 0.8f);
    private static readonly Color EmptyColor = new(0.13f, 0.11f, 0.12f);

    [MenuItem("Tools/Tidy HUD")]
    private static void Create()
    {
        var currency = Object.FindAnyObjectByType<CurrencyUI>(FindObjectsInactive.Include);
        Canvas canvas = currency != null ? currency.GetComponentInParent<Canvas>(true) : null;
        if (canvas == null)
        {
            Debug.LogWarning("Tidy HUD: open the game scene first (no HUD canvas found).");
            return;
        }
        canvas = canvas.rootCanvas;

        Sprite board = UILayoutSetup.LoadSprite(UILayoutSetup.BoardPath);
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(OutlineFontPath);
        var material = AssetDatabase.LoadAssetAtPath<Material>(OutlineMaterialPath);
        if (font == null)
            Debug.LogWarning($"Tidy HUD: no font at {OutlineFontPath}; the numbers keep the default font.");

        TidyGrassCounter(currency, canvas, board, font, material);
        RectTransform column = CreateHealthBars(canvas, board, font, material);
        TidyWeaponKeys(font, material);
        HideFloatingBars();
        MoveHeldItemsBelow(column);

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("Tidy HUD: done. Save the scene to keep it.");
    }

    // The grass counter keeps its object (the bonus grass flies to it) and its icon; its insides are rebuilt
    private static void TidyGrassCounter(CurrencyUI currency, Canvas canvas, Sprite board, TMP_FontAsset font, Material material)
    {
        var serialized = new SerializedObject(currency);
        var counter = serialized.FindProperty("counter").objectReferenceValue as RectTransform;
        if (counter == null)
            counter = canvas.transform.Find("GrassCounter") as RectTransform;
        if (counter == null)
        {
            Debug.LogWarning("Tidy HUD: no grass counter found; run Tools > Create UI Layout Objects first.");
            return;
        }

        Sprite icon = null;
        Transform oldIcon = counter.Find("Icon");
        if (oldIcon != null && oldIcon.TryGetComponent(out Image oldImage))
            icon = oldImage.sprite;
        for (int i = counter.childCount - 1; i >= 0; i--)
            Undo.DestroyObjectImmediate(counter.GetChild(i).gameObject);

        Undo.RecordObject(counter, "Tidy Grass Counter");
        counter.anchorMin = counter.anchorMax = counter.pivot = new Vector2(0f, 1f);
        counter.anchoredPosition = new Vector2(Margin, -Margin);
        counter.sizeDelta = new Vector2(GrassBoardWidth, BoardHeight) * PixelSize;

        CreateBoard(counter, board, canvas);
        CreateIcon(counter, icon);

        TextMeshProUGUI count = CreateNumber("Count", counter, Currency.Grass.ToString(), font, material);
        count.alignment = TextAlignmentOptions.Left;
        UILayoutSetup.Stretch(count.rectTransform, new Vector2(ContentLeft, 0f) * PixelSize,
                              new Vector2(-ContentRight, 0f) * PixelSize);

        serialized.FindProperty("counter").objectReferenceValue = counter;
        serialized.FindProperty("countText").objectReferenceValue = count;
        serialized.ApplyModifiedProperties();
    }

    // A column in the top-right corner, one row each: [icon on the board's end | bar with "18/20" over it]
    private static RectTransform CreateHealthBars(Canvas canvas, Sprite board, TMP_FontAsset font, Material material)
    {
        Transform old = canvas.transform.Find("HealthBars");
        if (old != null)
            Undo.DestroyObjectImmediate(old.gameObject); // rebuilt from scratch

        RectTransform column = UILayoutSetup.CreateRect("HealthBars", canvas.transform);
        Undo.RegisterCreatedObjectUndo(column.gameObject, "Tidy HUD");
        column.anchorMin = column.anchorMax = column.pivot = Vector2.one;
        column.anchoredPosition = new Vector2(-Margin, -Margin);
        column.sizeDelta = new Vector2(HealthBoardWidth, BoardHeight * 2f + RowGap) * PixelSize;

        CreateHealthRow(column, "Player", HudHealthBar.Owner.Player, PixelSprites.Import(PlayerIconPath), PlayerColor,
                        board, font, material, canvas, 0f);
        CreateHealthRow(column, "Castle", HudHealthBar.Owner.Castle, PixelSprites.Import(CastleIconPath), CastleColor,
                        board, font, material, canvas, -(BoardHeight + RowGap) * PixelSize);
        return column;
    }

    private static void CreateHealthRow(RectTransform column, string name, HudHealthBar.Owner owner, Sprite icon,
                                        Color color, Sprite board, TMP_FontAsset font, Material material, Canvas canvas, float y)
    {
        RectTransform row = UILayoutSetup.CreateRect(name, column);
        row.anchorMin = row.anchorMax = row.pivot = Vector2.one;
        row.anchoredPosition = new Vector2(0f, y);
        row.sizeDelta = new Vector2(HealthBoardWidth, BoardHeight) * PixelSize;

        CreateBoard(row, board, canvas);

        // On the wood, a pixel in from the frame all round (the wood is rows 3 to 11 from the bottom)
        RectTransform inner = UILayoutSetup.CreateRect("Inner", row);
        UILayoutSetup.Stretch(inner, new Vector2(ContentLeft, 4f) * PixelSize, -new Vector2(ContentRight + 1f, 5f) * PixelSize);
        Image empty = inner.gameObject.AddComponent<Image>();
        empty.color = EmptyColor;
        empty.raycastTarget = false;

        Image trail = UILayoutSetup.CreateImage("Trail", inner, null);
        trail.color = TrailColor;
        UILayoutSetup.Stretch(trail.rectTransform, Vector2.zero, Vector2.zero);
        Image fill = UILayoutSetup.CreateImage("Fill", inner, null);
        fill.color = color;
        UILayoutSetup.Stretch(fill.rectTransform, Vector2.zero, Vector2.zero);

        CreateIcon(row, icon); // over the board's end

        TextMeshProUGUI amount = CreateNumber("Amount", inner, "20/20", font, material);
        UILayoutSetup.Stretch(amount.rectTransform, Vector2.zero, Vector2.zero);

        var bar = Undo.AddComponent<HudHealthBar>(row.gameObject);
        var serialized = new SerializedObject(bar);
        serialized.FindProperty("owner").enumValueIndex = (int)owner;
        serialized.FindProperty("inner").objectReferenceValue = inner;
        serialized.FindProperty("fill").objectReferenceValue = fill;
        serialized.FindProperty("trail").objectReferenceValue = trail;
        serialized.FindProperty("amount").objectReferenceValue = amount;
        serialized.FindProperty("pixelSize").floatValue = PixelSize;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // The wood board filling its parent, its edges at the HUD's pixel size
    private static void CreateBoard(RectTransform parent, Sprite board, Canvas canvas)
    {
        Image image = UILayoutSetup.CreateImage("Board", parent, board);
        if (board != null)
        {
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = canvas.referencePixelsPerUnit / (board.pixelsPerUnit * PixelSize);
        }
        UILayoutSetup.Stretch(image.rectTransform, Vector2.zero, Vector2.zero);
    }

    // The icon at the HUD's pixel size, centred on the 12x12 spot at the board's left end, level with its frame
    private static void CreateIcon(RectTransform parent, Sprite icon)
    {
        Image image = UILayoutSetup.CreateImage("Icon", parent, icon);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2((IconLeft + IconSize * 0.5f) * PixelSize, 0f);
        rect.sizeDelta = Vector2.one * IconSize * PixelSize;
        UIPixels.Fit(image, PixelSize);
    }

    private static TextMeshProUGUI CreateNumber(string name, Transform parent, string content, TMP_FontAsset font, Material material)
    {
        TextMeshProUGUI text = UILayoutSetup.CreateText(name, parent, content, font, TextSize);
        Outline(text, font, material);
        return text;
    }

    // White with the dark pixel outline, like the wave's title
    private static void Outline(TMP_Text text, TMP_FontAsset font, Material material)
    {
        if (font != null)
            text.font = font;
        if (material != null)
            text.fontSharedMaterial = material;
        text.color = Color.white;
    }

    // The slots' number keys: the same white outlined numbers (they keep their size and place)
    private static void TidyWeaponKeys(TMP_FontAsset font, Material material)
    {
        var bar = Object.FindAnyObjectByType<WeaponBarUI>(FindObjectsInactive.Include);
        if (bar == null) return;
        foreach (TMP_Text text in bar.GetComponentsInChildren<TMP_Text>(true))
        {
            if (text.name != "Key") continue;
            Undo.RecordObject(text, "Tidy Weapon Keys");
            Outline(text, font, material);
        }
    }

    // The player's and castle's world-space bars (hidden, not deleted); enemies keep theirs
    private static void HideFloatingBars()
    {
        foreach (HealthBar bar in Object.FindObjectsByType<HealthBar>(FindObjectsInactive.Include))
        {
            bool ownedByPlayerOrCastle = bar.GetComponentInParent<Player>(true) != null
                                         || bar.GetComponentInParent<CastleHealth>(true) != null;
            if (!ownedByPlayerOrCastle) continue;
            Undo.RecordObject(bar.gameObject, "Hide Health Bar");
            bar.gameObject.SetActive(false);
        }
    }

    // The seed and Totem icons sit in the top-right corner too: down below the bars
    private static void MoveHeldItemsBelow(RectTransform column)
    {
        var held = Object.FindAnyObjectByType<HeldItemsUI>(FindObjectsInactive.Include);
        if (held == null) return;

        float top = -(Margin + column.sizeDelta.y + 2f * PixelSize);
        foreach (RectTransform icon in held.transform)
        {
            if (icon.anchorMax.y < 1f) continue; // only ones hung from the top
            Undo.RecordObject(icon, "Move Held Item");
            icon.anchoredPosition = new Vector2(icon.anchoredPosition.x, top);
        }
    }
}
