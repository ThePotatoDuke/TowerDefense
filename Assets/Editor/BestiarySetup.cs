using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Tools > Set Up Bestiary: makes or tops up Resources/Bestiary with every enemy prefab, in the order below,
// each with its frames (its data's idle or carry frames, its Animator's starting animation, or drawn icons
// for the 3D spike balls and roller) and a write-up. Entries already there keep what's in them (so their
// text can be edited); empty fields are filled.
// Tools > Create Bestiary (with the main menu or the game scene open): builds the bestiary panel on the
// canvas, and a Bestiary button to open it: on the main menu between Play and Quit (run Tools > Set Up Main
// Menu Intro after, to hang it on chains), on the pause panel under its buttons (run Tools > Add Pause
// Audio Toggles after, to move the toggles under it). Running it again rebuilds the panel.
public static class BestiarySetup
{
    private const string AssetPath = "Assets/Resources/Bestiary.asset";
    private const string EnemyFolder = "Assets/Prefabs/Enemies";
    private const string IconFolder = "Assets/3rd Party/Sprites/Enemies/";
    private const string BoardPath = "Assets/3rd Party/UI/game_over_board.png";
    private const string WindowPath = "Assets/3rd Party/UI/description_window.png";
    private const string PanelName = "BestiaryPanel";

    // Prefab name, shown name, what it does, a tip
    private static readonly (string prefab, string name, string description, string tip)[] Write =
    {
        ("BasicSlime", "Green Slime",
         "The most common slime. Hops steadily down the path towards the castle.",
         "One sword sweep can take out a whole line of them."),
        ("MiniSlime", "Mini Slime",
         "Small, weak and quick, and never alone: they come in tight swarms of a dozen or more.",
         "Swing through the pack. The sword hits every slime it touches."),
        ("ChargerSlime", "Charger Slime",
         "Sits still gathering itself, then leaps a long way down the path in one bound.",
         "Hit it while it's resting between leaps."),
        ("RhythmSlime", "Rhythm Slime",
         "Hops to a beat: two short hops, then a big, high leap that takes a while to come down.",
         "Time your swings for its landings."),
        ("TricksterSlime", "Trickster Slime",
         "Never goes straight. It hops back and forth at random, so it's hard to tell where it'll be next.",
         "Let it come to you; chasing it wastes time."),
        ("HyperSlime", "Hyper Slime",
         "Tiny and frantic, hopping without a pause. Fragile, but it hits hard, and it never carries a shield.",
         "Ranged weapons pick it off before it gets close."),
        ("HeavyLeaper", "Heavy Leaper",
         "A big, tough slime with slow, heavy hops. Lots of health, and it hurts to be landed on.",
         "Wear it down early; it takes a while to get through."),
        ("SkyHopperSlime", "Sky Hopper",
         "Bounds high into the air with every hop, covering a lot of ground at once.",
         "Strike as it lands."),
        ("SplitterSlime", "Splitter Slime",
         "Bursts into three Mini Slimes when it's killed.",
         "Have a swing ready for the pieces."),
        ("CheckerSlime", "Checker Slime",
         "Comes in black and white lines that leapfrog along the path: the slime at the back jumps over the rest, like a wheel rolling.",
         "Gaps in the line slow the whole group down."),
        ("SlimeQueen", "Slime Queen",
         "A boss. She throws out a brood of slimes each time she loses a fifth of her health, and a bigger one when she dies.",
         "Clear each brood before pushing her further."),
        ("BasicWalker", "Walker",
         "Walks steadily along the path. Nothing fancy, just sturdy.",
         ""),
        ("HeavyWalker", "Heavy Walker",
         "Slow, but with well over twice a Walker's health.",
         "Don't let a crowd build up behind it."),
        ("HyperWalker", "Hyper Walker",
         "Fast and frail, sprinting for the castle.",
         "Catch it early, before it's past you."),
        ("BasicBat", "Bat",
         "Flies low along the path, weaving from side to side.",
         "Lead your swings: it won't be where it is now."),
        ("PurpleBat", "Purple Bat",
         "Climbs high and dives back down as it flies. Up high, only the Blaster can aim at it.",
         "Hit it on the way down."),
        ("RedBat", "Red Bat",
         "Weaves and climbs at once, the hardest bat to pin down.",
         "The Blaster can reach it wherever it is."),
        ("GreenFrills", "Green Frills",
         "Floats high over the path at a steady pace, out of reach of all but the Blaster, shooting at you. A marker shows where each shot will land.",
         "Step off the marker before the shot lands."),
        ("PinkFrills", "Pink Frills",
         "A Frills that starts slow and speeds up as it nears the castle.",
         "Deal with it early, while it's slow."),
        ("WhiteFrills", "White Frills",
         "A Frills that rushes in fast, then slows down near the castle, shooting at you for longer.",
         "Keep moving near the castle."),
        ("Konga", "Konga",
         "Travels in long chains, each one following the one ahead. Break the chain and the ones behind rush forward to close the gap.",
         "Cutting the front of the chain holds up the rest for a moment."),
        ("Molly", "Molly",
         "A mole that tunnels along the path. Underground she can't be hit; she pops up to look around, then digs on. At the castle she stays up and attacks it.",
         "Be ready the moment she surfaces."),
        ("Tibbit", "Tibbit",
         "A little frog that rides other enemies' heads, hopping forward from one to the next. It can't be hit while riding; when its ride dies it falls off dazed and carries on alone.",
         "Catch it on the ground or mid-jump. Rollers flatten it too."),
        ("OrangeTibbit", "Orange Tibbit",
         "A quicker, frailer Tibbit. It barely stops on each ride, and dies along with whatever it's riding.",
         "Kill its ride and it goes too."),
        ("Roller", "Roller",
         "A spiked cylinder that drops onto the path and rolls back and forth along it for a few waves. Other enemies hop over it, spike balls get bumped along, Tibbits on the ground get flattened, and it mows the grass as it goes.",
         "Stay out of its way, and let it do some of your work."),
        ("SpikeBallBlack", "Spike Ball",
         "A big, slow spike ball that drops onto the path and rolls along with the enemies, soaking up shots meant for them. It can't be hurt, and leaves after a few waves.",
         "Aim around it, or fight where it isn't."),
        ("SpikeBallRed", "Red Spike Ball",
         "A fast spike ball that darts about among the enemies, shielding them from shots. It can't be hurt, and leaves after a few waves.",
         "Its contact hurts. Keep your distance."),
        ("SpikeBallYellow", "Yellow Spike Ball",
         "A wobbly spike ball that can't be hurt. When its time is up it bursts, firing spikes in every direction; red lines show where they'll fly.",
         "Step off the red lines before it bursts."),
        ("Bomberr", "Bomberr",
         "A bomb carried by a crew of little bomberrs. Beat it, or let it reach the castle, and they throw the bomb: it lands nearby and explodes after a short fuse, hurting enemies, you and the castle.",
         "Pop it in a crowd of enemies, then get clear."),
    };

    // Drawn icons for the enemies that are 3D models in the game
    private static readonly Dictionary<string, string> DrawnIcons = new()
    {
        { "SpikeBallBlack", "Walkers/menu_spike{0}.png" },
        { "SpikeBallRed", "Bestiary/spike_red{0}.png" },
        { "SpikeBallYellow", "Bestiary/spike_yellow{0}.png" },
        { "Roller", "Bestiary/roller{0}.png" },
    };

    [MenuItem("Tools/Set Up Bestiary")]
    private static void SetUpData()
    {
        var bestiary = AssetDatabase.LoadAssetAtPath<BestiaryData>(AssetPath);
        if (bestiary == null)
        {
            bestiary = ScriptableObject.CreateInstance<BestiaryData>();
            AssetDatabase.CreateAsset(bestiary, AssetPath);
        }

        Dictionary<string, EnemyBase> prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { EnemyFolder })
            .Select(guid => AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(guid)))
            .Where(prefab => prefab != null && prefab.GetComponent<EnemyBase>() != null)
            .ToDictionary(prefab => prefab.name, prefab => prefab.GetComponent<EnemyBase>());

        Undo.RecordObject(bestiary, "Set Up Bestiary");
        var ordered = new List<BestiaryData.Entry>();
        foreach ((string prefabName, string name, string description, string tip) in Write)
        {
            if (!prefabs.TryGetValue(prefabName, out EnemyBase prefab))
            {
                Debug.LogWarning($"Bestiary: no enemy prefab called {prefabName}.");
                continue;
            }
            ordered.Add(Fill(bestiary, prefab, name, description, tip));
            prefabs.Remove(prefabName);
        }
        // Enemies added since, at the end, to be written up by hand
        foreach (EnemyBase prefab in prefabs.Values)
        {
            ordered.Add(Fill(bestiary, prefab, ObjectNames.NicifyVariableName(prefab.name), "", ""));
            Debug.Log($"Bestiary: {prefab.name} added at the end, with no write-up yet.");
        }
        // Entries kept for prefabs no longer found are dropped
        bestiary.entries = ordered;

        EditorUtility.SetDirty(bestiary);
        AssetDatabase.SaveAssets();
        Selection.activeObject = bestiary;
        int framed = ordered.Count(entry => entry.frames != null && entry.frames.Length > 0);
        Debug.Log($"Bestiary: {ordered.Count} enemies in {AssetPath}, {framed} with frames.");
    }

    private static BestiaryData.Entry Fill(BestiaryData bestiary, EnemyBase prefab, string name, string description, string tip)
    {
        BestiaryData.Entry entry = bestiary.entries.Find(existing => existing.prefab == prefab)
                                   ?? new BestiaryData.Entry { prefab = prefab };
        if (string.IsNullOrEmpty(entry.displayName)) entry.displayName = name;
        if (string.IsNullOrEmpty(entry.description)) entry.description = description;
        if (string.IsNullOrEmpty(entry.tip)) entry.tip = tip;
        // Drawn icons are always taken afresh, so frames added to them are picked up
        if (DrawnIcons.ContainsKey(prefab.name) || entry.frames == null || entry.frames.Length == 0 ||
            entry.frames.Any(frame => frame == null))
            (entry.frames, entry.framesPerSecond) = FramesFor(prefab);
        return entry;
    }

    private static (Sprite[], float) FramesFor(EnemyBase prefab)
    {
        if (DrawnIcons.TryGetValue(prefab.name, out string pattern))
        {
            // Numbered from 1, as many as there are
            var drawn = new List<Sprite>();
            for (int i = 1; System.IO.File.Exists(IconFolder + string.Format(pattern, i)); i++)
            {
                Sprite frame = PixelSprites.Import(IconFolder + string.Format(pattern, i));
                if (frame != null) drawn.Add(frame);
            }
            return (drawn.ToArray(), 8f);
        }

        // Frames kept in its data (Frills, Tibbits and Mollies idling, the Bomberr's crew carrying)
        if (prefab.Data != null)
        {
            var data = new SerializedObject(prefab.Data);
            foreach ((string frames, string fps) in new[] { ("idleFrames", "idleFramesPerSecond"), ("carryFrames", "carryFramesPerSecond") })
            {
                SerializedProperty list = data.FindProperty(frames);
                if (list == null || !list.isArray || list.arraySize == 0) continue;
                Sprite[] sprites = Enumerable.Range(0, list.arraySize)
                    .Select(i => list.GetArrayElementAtIndex(i).objectReferenceValue as Sprite)
                    .Where(sprite => sprite != null).ToArray();
                if (sprites.Length == 0) continue;
                SerializedProperty rate = data.FindProperty(fps);
                return (sprites, rate != null ? rate.floatValue : 8f);
            }
        }

        (Sprite[] animated, float animatedFps) = MenuCrittersSetup.AnimatorFrames(prefab.gameObject);
        if (animated != null && animated.Length > 0)
            return (animated, animatedFps);

        var sprite = prefab.GetComponentInChildren<SpriteRenderer>(true);
        if (sprite != null && sprite.sprite != null)
            return (new[] { sprite.sprite }, 8f);
        Debug.LogWarning($"Bestiary: no frames found for {prefab.name}; give its entry some by hand.");
        return (new Sprite[0], 8f);
    }

    [MenuItem("Tools/Create Bestiary")]
    private static void CreatePanel()
    {
        var menu = Object.FindAnyObjectByType<MainMenu>(FindObjectsInactive.Include);
        var pause = Object.FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include);
        Canvas canvas = menu != null ? menu.GetComponentInParent<Canvas>()
                      : pause != null ? pause.GetComponentInParent<Canvas>() : null;
        if (canvas == null)
        {
            Debug.LogWarning("Bestiary: open the main menu, or the game scene with its pause menu (Tools > Create Pause Menu).");
            return;
        }
        if (AssetDatabase.LoadAssetAtPath<BestiaryData>(AssetPath) == null)
            SetUpData();

        canvas = canvas.rootCanvas;
        Transform old = canvas.transform.Find(PanelName);
        if (old != null)
            Undo.DestroyObjectImmediate(old.gameObject);

        BestiaryPanel bestiary = canvas.GetComponent<BestiaryPanel>();
        if (bestiary == null)
            bestiary = Undo.AddComponent<BestiaryPanel>(canvas.gameObject);
        GameObject panel = BuildPanel(canvas, bestiary);

        string where = menu != null ? AddMenuButton(canvas, bestiary) : AddPauseButton(pause, bestiary);

        panel.SetActive(false);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);
        Selection.activeObject = panel;
        Debug.Log($"Bestiary: panel made (switched off; tick it on to see it). {where} Then save the scene.");
    }

    private static GameObject BuildPanel(Canvas canvas, BestiaryPanel bestiary)
    {
        TMP_FontAsset titleFont = UILayoutSetup.GetFontAsset(UILayoutSetup.TitleFontPath);
        TMP_FontAsset textFont = UILayoutSetup.GetFontAsset(UILayoutSetup.TextFontPath);
        Sprite boardSprite = UILayoutSetup.LoadSprite(BoardPath);
        Sprite window = UILayoutSetup.LoadSprite(WindowPath);
        Sprite buttonSprite = UILayoutSetup.LoadSprite(UILayoutSetup.BoardPath);

        // Full screen, darkening the game and catching every click
        Image dim = UILayoutSetup.CreateImage(PanelName, canvas.transform, null);
        dim.color = new Color(0f, 0f, 0f, 0.65f);
        dim.raycastTarget = true;
        UILayoutSetup.Stretch(dim.rectTransform, Vector2.zero, Vector2.zero);
        GameObject panel = dim.gameObject;

        Image board = UILayoutSetup.CreateImage("Board", panel.transform, boardSprite);
        UIPixels.Slice(board, canvas);
        board.raycastTarget = true;
        Place(board.rectTransform, Vector2.zero, new Vector2(1560f, 920f));

        TextMeshProUGUI title = UILayoutSetup.CreateText("Title", board.transform, "BESTIARY", titleFont, 72f);
        Place(title.rectTransform, new Vector2(0f, 390f), new Vector2(1000f, 90f));
        TextMeshProUGUI progress = UILayoutSetup.CreateText("Progress", board.transform, "0 / 0 met", textFont, 30f);
        progress.alignment = TextAlignmentOptions.Right;
        Place(progress.rectTransform, new Vector2(560f, 390f), new Vector2(340f, 60f));

        // The grid, scrolling, on the left
        RectTransform scrollRect = UILayoutSetup.CreateRect("Scroll", board.transform);
        Place(scrollRect, new Vector2(-385f, -10f), new Vector2(690f, 680f));
        var scroll = scrollRect.gameObject.AddComponent<ScrollRect>();
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 40f;
        RectTransform viewport = UILayoutSetup.CreateRect("Viewport", scrollRect);
        UILayoutSetup.Stretch(viewport, Vector2.zero, Vector2.zero);
        viewport.gameObject.AddComponent<RectMask2D>();
        var viewportImage = viewport.gameObject.AddComponent<Image>(); // something to scroll with the wheel over
        viewportImage.color = new Color(1f, 1f, 1f, 0f);
        RectTransform grid = UILayoutSetup.CreateRect("Grid", viewport);
        grid.anchorMin = new Vector2(0f, 1f);
        grid.anchorMax = new Vector2(1f, 1f);
        grid.pivot = new Vector2(0.5f, 1f);
        grid.anchoredPosition = Vector2.zero;
        grid.sizeDelta = Vector2.zero;
        var layout = grid.gameObject.AddComponent<GridLayoutGroup>();
        layout.cellSize = new Vector2(120f, 120f);
        layout.spacing = new Vector2(16f, 16f);
        layout.padding = new RectOffset(12, 12, 12, 12);
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = 5;
        layout.childAlignment = TextAnchor.UpperCenter;
        grid.gameObject.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        scroll.viewport = viewport;
        scroll.content = grid;

        // One square, copied for each enemy
        Image square = UILayoutSetup.CreateImage("Entry", grid, window);
        UIPixels.Slice(square, canvas);
        square.raycastTarget = true;
        var squareButton = square.gameObject.AddComponent<Button>();
        squareButton.targetGraphic = square;
        Image picked = UILayoutSetup.CreateImage("Picked", square.transform, window);
        UIPixels.Slice(picked, canvas);
        picked.color = new Color(1f, 0.85f, 0.35f, 0.55f);
        UILayoutSetup.Stretch(picked.rectTransform, Vector2.zero, Vector2.zero);
        Image icon = UILayoutSetup.CreateImage("Icon", square.transform, null);
        Place(icon.rectTransform, Vector2.zero, new Vector2(96f, 96f));
        var entry = square.gameObject.AddComponent<BestiaryEntryButton>();
        var entryData = new SerializedObject(entry);
        entryData.FindProperty("icon").objectReferenceValue = icon;
        entryData.FindProperty("pickedMark").objectReferenceValue = picked.gameObject;
        entryData.ApplyModifiedPropertiesWithoutUndo();

        // The picked one's details on the right
        Image details = UILayoutSetup.CreateImage("Details", board.transform, window);
        UIPixels.Slice(details, canvas);
        Place(details.rectTransform, new Vector2(385f, -10f), new Vector2(690f, 680f));
        Image portrait = UILayoutSetup.CreateImage("Portrait", details.transform, null);
        Place(portrait.rectTransform, new Vector2(0f, 200f), new Vector2(220f, 220f));
        TextMeshProUGUI nameText = UILayoutSetup.CreateText("Name", details.transform, "Name", titleFont, 52f);
        Place(nameText.rectTransform, new Vector2(0f, 50f), new Vector2(620f, 64f));
        TextMeshProUGUI stats = UILayoutSetup.CreateText("Stats", details.transform, "Health 0    Damage 0", textFont, 26f);
        stats.textWrappingMode = TextWrappingModes.Normal;
        Place(stats.rectTransform, new Vector2(0f, -15f), new Vector2(620f, 70f));
        TextMeshProUGUI description = UILayoutSetup.CreateText("Description", details.transform, "What it does.", textFont, 26f);
        description.textWrappingMode = TextWrappingModes.Normal;
        description.alignment = TextAlignmentOptions.TopLeft;
        Place(description.rectTransform, new Vector2(0f, -150f), new Vector2(620f, 190f));
        TextMeshProUGUI tip = UILayoutSetup.CreateText("Tip", details.transform, "Tip: how to deal with it.", textFont, 24f);
        tip.textWrappingMode = TextWrappingModes.Normal;
        tip.alignment = TextAlignmentOptions.TopLeft;
        tip.color = Color.Lerp(UILayoutSetup.TextColor, new Color(0.2f, 0.45f, 0.2f), 0.6f);
        Place(tip.rectTransform, new Vector2(0f, -285f), new Vector2(620f, 80f));

        // Back, along the bottom
        Image back = UILayoutSetup.CreateImage("Back", board.transform, buttonSprite);
        UIPixels.Slice(back, canvas);
        back.raycastTarget = true;
        Place(back.rectTransform, new Vector2(0f, -400f), new Vector2(240f, 70f));
        var backButton = back.gameObject.AddComponent<Button>();
        backButton.targetGraphic = back;
        TextMeshProUGUI backLabel = UILayoutSetup.CreateText("Label", back.transform, "Back", textFont, 32f);
        UILayoutSetup.Stretch(backLabel.rectTransform, Vector2.zero, Vector2.zero);

        var serialized = new SerializedObject(bestiary);
        serialized.FindProperty("panel").objectReferenceValue = panel;
        serialized.FindProperty("board").objectReferenceValue = board.rectTransform;
        serialized.FindProperty("scroll").objectReferenceValue = scroll;
        serialized.FindProperty("grid").objectReferenceValue = grid;
        serialized.FindProperty("entryTemplate").objectReferenceValue = entry;
        serialized.FindProperty("closeButton").objectReferenceValue = backButton;
        serialized.FindProperty("progressText").objectReferenceValue = progress;
        serialized.FindProperty("portrait").objectReferenceValue = portrait;
        serialized.FindProperty("nameText").objectReferenceValue = nameText;
        serialized.FindProperty("statsText").objectReferenceValue = stats;
        serialized.FindProperty("descriptionText").objectReferenceValue = description;
        serialized.FindProperty("tipText").objectReferenceValue = tip;
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return panel;
    }

    // A copy of Quit, between Play and Quit
    private static string AddMenuButton(Canvas canvas, BestiaryPanel bestiary)
    {
        Transform root = canvas.transform;
        if (root.Find("Bestiary") is Transform existing && existing.GetComponent<Button>() != null)
        {
            Wire(existing.GetComponent<Button>(), bestiary);
            return "The menu's Bestiary button was already there.";
        }
        Button quit = root.Find("Quit")?.GetComponent<Button>();
        if (quit == null) return "No Quit button found to copy for the menu; add a button calling BestiaryPanel.Open.";

        Button button = Object.Instantiate(quit, root);
        Undo.RegisterCreatedObjectUndo(button.gameObject, "Create Bestiary");
        button.name = "Bestiary";
        button.transform.SetSiblingIndex(quit.transform.GetSiblingIndex());
        Wire(button, bestiary);
        return "Added a Bestiary button to the menu: run Tools > Set Up Main Menu Intro to hang it between Play and Quit.";
    }

    // A copy of Resume, centred under the pause panel's buttons
    private static string AddPauseButton(PauseMenu pause, BestiaryPanel bestiary)
    {
        var pauseData = new SerializedObject(pause);
        var panel = pauseData.FindProperty("panel").objectReferenceValue as GameObject;
        var resume = pauseData.FindProperty("resumeButton").objectReferenceValue as Button;
        if (panel == null || resume == null) return "No pause panel with a Resume button to copy; add a button calling BestiaryPanel.Open.";

        Transform existing = resume.transform.parent.Find("Bestiary");
        if (existing != null && existing.GetComponent<Button>() != null)
        {
            Wire(existing.GetComponent<Button>(), bestiary);
            return "The pause panel's Bestiary button was already there.";
        }

        // Under the lowest of the row (not the audio toggles)
        var rowButton = (RectTransform)resume.transform;
        RectTransform parent = (RectTransform)rowButton.parent;
        float below = float.MaxValue;
        var corners = new Vector3[4];
        foreach (Button other in panel.GetComponentsInChildren<Button>(true))
        {
            if (other.GetComponent<AudioToggle>() != null) continue;
            ((RectTransform)other.transform).GetWorldCorners(corners);
            below = Mathf.Min(below, parent.InverseTransformPoint(corners[0]).y);
        }

        Button button = Object.Instantiate(resume, parent);
        Undo.RegisterCreatedObjectUndo(button.gameObject, "Create Bestiary");
        button.name = "Bestiary";
        Wire(button, bestiary);
        var rect = (RectTransform)button.transform;
        float gap = rowButton.rect.height * 0.25f;
        Vector3 position = rect.localPosition;
        position.x = 0f;
        position.y = below - gap - rect.rect.height * (1f - rect.pivot.y);
        rect.localPosition = position;
        return "Added a Bestiary button under the pause panel's buttons: run Tools > Add Pause Audio Toggles to move the toggles under it.";
    }

    // Opens the bestiary (and nothing else), labelled Bestiary
    private static void Wire(Button button, BestiaryPanel bestiary)
    {
        Undo.RecordObject(button, "Create Bestiary");
        while (button.onClick.GetPersistentEventCount() > 0)
            UnityEventTools.RemovePersistentListener(button.onClick, 0);
        UnityEventTools.AddPersistentListener(button.onClick, new UnityAction(bestiary.Open));
        TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);
        if (label != null)
        {
            Undo.RecordObject(label, "Create Bestiary");
            label.text = "Bestiary";
        }
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
