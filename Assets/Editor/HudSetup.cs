using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tools > Create HUD Health Bars: puts the player's and the castle's health in the top-right corner of the
// open scene's HUD (an icon and a board with a bar and "18/20" in it, one row each, at the UI pixel scale),
// switches off the health bars floating over the player and the castle (enemies keep theirs), and moves
// the held seed and Totem icons down below the bars. Run it again after deleting HealthBars to rebuild.
public static class HudSetup
{
    private const string PlayerIconPath = "Assets/3rd Party/UI/upgrades/player/Bouque_of_life.png";
    private const string CastleIconPath = "Assets/3rd Party/UI/upgrades/castle/castle_heal.png";

    // In art pixels at the UI's pixel scale
    private static readonly Vector2 BoardPixels = new(64f, 16f);
    private const float IconPixels = 16f;
    private const float Margin = 24f;   // from the screen's edges, in canvas units
    private const float RowGap = 2f;    // between the rows, in art pixels
    private const float IconGap = 2f;   // between an icon and its board, in art pixels

    private static readonly Color PlayerColor = new(0.36f, 0.78f, 0.36f);
    private static readonly Color CastleColor = new(0.35f, 0.62f, 0.9f);
    private static readonly Color TrailColor = new(1f, 0.95f, 0.8f);
    private static readonly Color EmptyColor = new(0.13f, 0.11f, 0.12f);

    [MenuItem("Tools/Create HUD Health Bars")]
    private static void Create()
    {
        var currency = Object.FindAnyObjectByType<CurrencyUI>(FindObjectsInactive.Include);
        Canvas canvas = currency != null ? currency.GetComponentInParent<Canvas>(true) : null;
        if (canvas == null)
        {
            Debug.LogWarning("HUD health bars: open the game scene first (no HUD canvas found).");
            return;
        }
        canvas = canvas.rootCanvas;
        if (canvas.transform.Find("HealthBars") != null)
        {
            Debug.Log("HUD health bars: HealthBars already exists; delete it first to rebuild it.");
            return;
        }

        Sprite board = UILayoutSetup.LoadSprite(UILayoutSetup.BoardPath);
        TMP_FontAsset font = UILayoutSetup.GetFontAsset(UILayoutSetup.TextFontPath);
        float scale = UIPixels.Scale;

        // A column in the top-right corner
        RectTransform column = UILayoutSetup.CreateRect("HealthBars", canvas.transform);
        column.anchorMin = column.anchorMax = column.pivot = Vector2.one;
        column.anchoredPosition = new Vector2(-Margin, -Margin);
        float rowHeight = BoardPixels.y * scale;
        column.sizeDelta = new Vector2((IconPixels + IconGap + BoardPixels.x) * scale, rowHeight * 2f + RowGap * scale);

        CreateRow(column, "Player", HudHealthBar.Owner.Player, PixelSprites.Import(PlayerIconPath), PlayerColor,
                  board, font, canvas, 0f);
        CreateRow(column, "Castle", HudHealthBar.Owner.Castle, PixelSprites.Import(CastleIconPath), CastleColor,
                  board, font, canvas, -(rowHeight + RowGap * scale));

        HideFloatingBars();
        MoveHeldItemsBelow(column);

        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Debug.Log("HUD health bars: made HealthBars in the top-right corner. Save the scene to keep them.");
    }

    // [icon][board: trail and fill inside its border, the amount over them]
    private static void CreateRow(RectTransform column, string name, HudHealthBar.Owner owner, Sprite icon, Color color,
                                  Sprite board, TMP_FontAsset font, Canvas canvas, float y)
    {
        float scale = UIPixels.Scale;
        RectTransform row = UILayoutSetup.CreateRect(name, column);
        row.anchorMin = row.anchorMax = row.pivot = Vector2.one;
        row.anchoredPosition = new Vector2(0f, y);
        row.sizeDelta = new Vector2(column.sizeDelta.x, BoardPixels.y * scale);

        Image iconImage = UILayoutSetup.CreateImage("Icon", row, icon);
        UIPixels.Fit(iconImage);
        RectTransform iconRect = iconImage.rectTransform;
        iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;

        Image boardImage = UILayoutSetup.CreateImage("Board", row, board);
        UIPixels.Slice(boardImage, canvas);
        RectTransform boardRect = boardImage.rectTransform;
        boardRect.anchorMin = boardRect.anchorMax = boardRect.pivot = new Vector2(1f, 0.5f);
        boardRect.anchoredPosition = Vector2.zero;
        boardRect.sizeDelta = BoardPixels * scale;

        // Inside the board's border
        Vector4 border = board != null ? board.border : Vector4.zero; // left, bottom, right, top
        RectTransform inner = UILayoutSetup.CreateRect("Inner", boardRect);
        UILayoutSetup.Stretch(inner, new Vector2(border.x, border.y) * scale, -new Vector2(border.z, border.w) * scale);
        Image empty = inner.gameObject.AddComponent<Image>();
        empty.color = EmptyColor;
        empty.raycastTarget = false;

        Image trail = UILayoutSetup.CreateImage("Trail", inner, null);
        trail.color = TrailColor;
        UILayoutSetup.Stretch(trail.rectTransform, Vector2.zero, Vector2.zero);
        Image fill = UILayoutSetup.CreateImage("Fill", inner, null);
        fill.color = color;
        UILayoutSetup.Stretch(fill.rectTransform, Vector2.zero, Vector2.zero);

        TextMeshProUGUI amount = UILayoutSetup.CreateText("Amount", boardRect, "20/20", font, 28f);
        UILayoutSetup.Stretch(amount.rectTransform, Vector2.zero, Vector2.zero);

        var bar = Undo.AddComponent<HudHealthBar>(row.gameObject);
        var serialized = new SerializedObject(bar);
        serialized.FindProperty("owner").enumValueIndex = (int)owner;
        serialized.FindProperty("inner").objectReferenceValue = inner;
        serialized.FindProperty("fill").objectReferenceValue = fill;
        serialized.FindProperty("trail").objectReferenceValue = trail;
        serialized.FindProperty("amount").objectReferenceValue = amount;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // The player's and castle's world-space bars (hidden, not deleted); enemies keep theirs
    private static void HideFloatingBars()
    {
        foreach (HealthBar bar in Object.FindObjectsByType<HealthBar>(FindObjectsInactive.Include, FindObjectsSortMode.None))
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

        float top = -(Margin + column.sizeDelta.y + RowGap * UIPixels.Scale * 2f);
        foreach (RectTransform icon in held.transform)
        {
            if (icon.anchorMax.y < 1f) continue; // only ones hung from the top
            Undo.RecordObject(icon, "Move Held Item");
            icon.anchoredPosition = new Vector2(icon.anchoredPosition.x, top);
        }
    }
}
