using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tools > Create Shop: makes Resources/ShopSettings with every upgrade at starting values (icons imported
// as pixel-art sprites, and the sprites, prefabs and projectile of the things bought: Spore Mines, the
// Totem, Crimson Weed, whose prefabs it makes in Prefabs/Shop) and builds, on the UI canvas of the open
// scene, the shop window (a dimmed background, the "Shop" title, three cards, and the reroll and leave
// buttons) and the icons for a held Totem (top right) or seed.
// Safe to run again: anything already set or built is left alone, so it only adds what's missing.
public static class ShopSetup
{
    private const string SettingsPath = "Assets/Resources/ShopSettings.asset";
    private const string IconFolder = "Assets/3rd Party/UI/upgrades/";
    private const string WiltedPath = "Assets/3rd Party/Sprites/Environment/wilted_stubs.png";
    private const string PrefabFolder = "Assets/Prefabs/Shop";
    private const string BlasterDataPath = "Assets/ScriptableObjects/Weapons/Ranged/RangedWeapons/DefaultGun.asset";
    private const string MushroomName = "GRASS+_334";
    private const string TotemName = "GRASS+_329";
    private static readonly string[] WeedStageNames = { "GRASS+_335", "GRASS+_336", "GRASS+_337" };
    private const float PixelScale = UIPixels.Scale; // screen units per board pixel

    private static readonly Color LightText = new Color(1f, 0.96f, 0.86f);

    private struct Default
    {
        public UpgradeType type;
        public string name, icon, description;
        public float value;
        public bool percent;
        public int maxLevel, cost;
        public float growth, weight;
    }

    // Starting numbers, to be balanced in the ShopSettings asset
    private static readonly Default[] Defaults =
    {
        Make(UpgradeType.MaxHealth, "Bouquet of Life", "player/Bouquet_of_life.png", "{value} max health, filled straight away", 2, false, 5, 40, 1.5f),
        Make(UpgradeType.Heal, "Heal", "player/player_heal.png", "Heals {value} of your health", 50, true, 0, 25, 1f),
        Make(UpgradeType.MoveSpeed, "Swift Feet", "player/player_speed.png", "Move {value} faster", 10, true, 5, 30, 1.4f),
        Make(UpgradeType.GrassMagnet, "Grass Magnet", "player/grass_magnet.png", "Grass flies to you from {value} further away", 40, true, 5, 25, 1.4f),
        Make(UpgradeType.SwordDamage, "Sword Damage", "sword/sword_damage.png", "Sword hits {value} harder", 20, true, 5, 35, 1.4f),
        Make(UpgradeType.SwordAttackRate, "Sword Speed", "sword/sword_attack_rate.png", "Sword swings {value} faster", 15, true, 5, 35, 1.4f),
        Make(UpgradeType.SwordSize, "Sword Size", "sword/sword_size.png", "Sword {value} bigger, reaching further", 15, true, 4, 35, 1.4f),
        Make(UpgradeType.BlasterDamage, "Blaster Damage", "player/blaster_damage.png", "Blaster shots hit {value} harder", 20, true, 5, 35, 1.4f),
        Make(UpgradeType.BlasterFireRate, "Blaster Fire Rate", "player/blaster_fire_rate.png", "Blaster fires {value} faster", 15, true, 5, 35, 1.4f),
        Make(UpgradeType.BlasterRange, "Blaster Range", "player/blaster_range.png", "Blaster reaches {value} further", 15, true, 4, 30, 1.4f),
        Make(UpgradeType.HoopCannonDamage, "Hoop Cannon Damage", "player/hoop_cannon_damage.png", "Hoops hit {value} harder", 20, true, 5, 35, 1.4f),
        Make(UpgradeType.HoopCannonFireRate, "Hoop Cannon Fire Rate", "player/hoop_cannon_fire_rate.png", "Hoop Cannon fires {value} faster", 15, true, 5, 35, 1.4f),
        Make(UpgradeType.HoopCannonRange, "Hoop Cannon Range", "player/hoop_cannon_range.png", "Hoop Cannon reaches {value} further", 15, true, 4, 30, 1.4f),
        Make(UpgradeType.CastleHeal, "Castle Repair", "castle/castle_heal.png", "Repairs {value} of the castle", 25, true, 0, 30, 1f),
        Make(UpgradeType.DropRate, "Lush Grass", "drop_rate.png", "{value} grass blade from every tuft mown", 1, false, 4, 40, 1.5f),
        Make(UpgradeType.GrowthRate, "Fast Growth", "growth_rate.png", "Mown grass grows back {value} faster", 25, true, 4, 30, 1.4f),
        Make(UpgradeType.SavingsBloom, "Savings Bloom", "other/savings_bloom.png", "Unspent grass grows {value} each time the shop closes", 10, true, 3, 50, 1.5f, 0.7f),
        Make(UpgradeType.SporeMines, "Spore Mines", "other/spore_mines.png", "{value} mushroom at the castle. They burst when an enemy reaches it", 1, false, 6, 45, 1.35f),
        Make(UpgradeType.Totem, "Totem", "other/totem.png", "Stands by the castle. Press T to warp back to it (once)", 0, false, 0, 120, 1f, 0.25f),
        Make(UpgradeType.CrimsonWeed, "Crimson Weed", "other/crimson_weed.png", "A seed to plant by the path (E). It shoots, and grows as it kills", 0, false, 0, 60, 1.3f, 0.8f),
    };

    private static Default Make(UpgradeType type, string name, string icon, string description, float value, bool percent,
                                int maxLevel, int cost, float growth, float weight = 1f) =>
        new Default { type = type, name = name, icon = icon, description = description, value = value, percent = percent,
                      maxLevel = maxLevel, cost = cost, growth = growth, weight = weight };

    [MenuItem("Tools/Create Shop")]
    private static void Create()
    {
        CreateSettings();

        var currency = Object.FindAnyObjectByType<CurrencyUI>(FindObjectsInactive.Include);
        if (currency != null)
        {
            CreateWindow(currency);
            CreateHeldItems(currency);
        }
        else
            Debug.LogWarning("Shop: no CurrencyUI in the open scene, so there's no canvas to put the shop window on.");

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Shop created. Save the scene to keep the window; numbers are in Assets/Resources/ShopSettings.");
    }

    // Swaps the held-item icons for a fresh copy in the current layout
    [MenuItem("Tools/Rebuild Held Item Icons")]
    private static void RebuildHeldItems()
    {
        var currency = Object.FindAnyObjectByType<CurrencyUI>(FindObjectsInactive.Include);
        if (currency == null)
        {
            Debug.LogWarning("Shop: no CurrencyUI in the open scene, so there's no canvas to put the icons on.");
            return;
        }

        var old = Object.FindAnyObjectByType<HeldItemsUI>(FindObjectsInactive.Include);
        if (old != null)
            Undo.DestroyObjectImmediate(old.gameObject);
        CreateHeldItems(currency);

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
        Debug.Log("Held item icons rebuilt. Save the scene to keep them.");
    }

    private static void CreateSettings()
    {
        var settings = AssetDatabase.LoadAssetAtPath<ShopSettings>(SettingsPath);
        if (settings == null)
        {
            settings = ScriptableObject.CreateInstance<ShopSettings>();
            AssetDatabase.CreateAsset(settings, SettingsPath);
        }

        foreach (Default entry in Defaults)
        {
            if (settings.Get(entry.type) != null) continue;
            settings.upgrades.Add(new UpgradeDefinition
            {
                type = entry.type,
                displayName = entry.name,
                icon = LoadIcon(entry.icon),
                description = entry.description,
                valuePerLevel = entry.value,
                isPercent = entry.percent,
                maxLevel = entry.maxLevel,
                baseCost = entry.cost,
                costGrowth = entry.growth,
                weight = entry.weight,
            });
        }

        FillBoughtThings(settings);

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
    }

    // Sprites and prefabs for the things bought, where they aren't set yet
    private static void FillBoughtThings(ShopSettings settings)
    {
        Sprite[] sheet = AssetDatabase.LoadAllAssetsAtPath(UILayoutSetup.GrassSheetPath).OfType<Sprite>().ToArray();
        Sprite FromSheet(string spriteName) => sheet.FirstOrDefault(sprite => sprite.name == spriteName);

        if (settings.sporeMine == null)
        {
            Sprite mushroom = FromSheet(MushroomName);
            Sprite wilted = LoadWorldSprite(WiltedPath);
            GameObject minePrefab = MakePrefab("SporeMine", mushroom, root => SetUpMine(root, mushroom, wilted));
            settings.sporeMine = minePrefab != null ? minePrefab.GetComponent<SporeMine>() : null;
        }
        if (settings.totem == null)
        {
            GameObject totemPrefab = MakePrefab("Totem", FromSheet(TotemName), root => root.AddComponent<Totem>());
            settings.totem = totemPrefab != null ? totemPrefab.GetComponent<Totem>() : null;
        }
        if (settings.crimsonWeed == null)
        {
            GameObject weedPrefab = MakePrefab("CrimsonWeed", FromSheet(WeedStageNames[0]), SetUpWeed);
            settings.crimsonWeed = weedPrefab != null ? weedPrefab.GetComponent<CrimsonWeed>() : null;
        }
        if (settings.crimsonWeed != null)
            FillWeed(settings.crimsonWeed, WeedStageNames.Select(FromSheet).ToArray());
    }

    // The weed prefab's stage sprites and shots (the Blaster's), where they aren't set yet
    private static void FillWeed(CrimsonWeed weed, Sprite[] stages)
    {
        var serialized = new SerializedObject(weed);

        SerializedProperty sprites = serialized.FindProperty("stageSprites");
        bool missing = sprites.arraySize != stages.Length;
        for (int i = 0; !missing && i < sprites.arraySize; i++)
            missing = sprites.GetArrayElementAtIndex(i).objectReferenceValue == null;
        if (missing)
        {
            sprites.arraySize = stages.Length;
            for (int i = 0; i < stages.Length; i++)
                sprites.GetArrayElementAtIndex(i).objectReferenceValue = stages[i];
        }

        var blaster = AssetDatabase.LoadAssetAtPath<RangedWeaponDataSO>(BlasterDataPath);
        SerializedProperty projectile = serialized.FindProperty("projectilePrefab");
        SerializedProperty data = serialized.FindProperty("projectileData");
        if (blaster != null && projectile.objectReferenceValue == null)
            projectile.objectReferenceValue = blaster.projectilePrefab;
        if (blaster != null && data.objectReferenceValue == null)
            data.objectReferenceValue = blaster.projectileData;

        if (serialized.ApplyModifiedPropertiesWithoutUndo())
        {
            EditorUtility.SetDirty(weed.gameObject);
            AssetDatabase.SaveAssets();
        }
    }

    // A prop prefab in Prefabs/Shop: the camera-facing root with the sprite as a child resting on the ground,
    // SpriteFX and a blob shadow. An existing one is kept as it is.
    private static GameObject MakePrefab(string name, Sprite sprite, System.Action<GameObject> setUp)
    {
        string path = $"{PrefabFolder}/{name}.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        if (!AssetDatabase.IsValidFolder(PrefabFolder))
            AssetDatabase.CreateFolder("Assets/Prefabs", "Shop");

        GameObject root = WorldProp.Build(name, sprite);
        root.AddComponent<SpriteFX>();
        root.AddComponent<BlobShadow>();
        setUp?.Invoke(root);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        Debug.Log($"Shop: made {path}");
        return prefab;
    }

    // The mushroom's script, with its sprite and the one it shows once burst
    private static void SetUpMine(GameObject root, Sprite mushroom, Sprite wilted)
    {
        var mine = root.AddComponent<SporeMine>();
        var serialized = new SerializedObject(mine);
        serialized.FindProperty("sprite").objectReferenceValue = root.GetComponentInChildren<SpriteRenderer>();
        serialized.FindProperty("mushroomSprite").objectReferenceValue = mushroom;
        serialized.FindProperty("wiltedSprite").objectReferenceValue = wilted;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // The weed's script, pointed at its sprite and at a muzzle half a unit up for the shots (its stage
    // sprites and shots are filled in by FillWeed)
    private static void SetUpWeed(GameObject root)
    {
        var muzzle = new GameObject("Muzzle").transform;
        muzzle.SetParent(root.transform, false);
        muzzle.localPosition = Vector3.up * 0.5f;

        var weed = root.AddComponent<CrimsonWeed>();
        var serialized = new SerializedObject(weed);
        serialized.FindProperty("sprite").objectReferenceValue = root.GetComponentInChildren<SpriteRenderer>();
        serialized.FindProperty("muzzle").objectReferenceValue = muzzle;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    // A sprite standing in the world, imported like the other environment sprites (16 pixels per unit,
    // crisp)
    private static Sprite LoadWorldSprite(string path)
    {
        if (AssetImporter.GetAtPath(path) is TextureImporter importer && importer.textureType != TextureImporterType.Sprite)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning($"Shop: no sprite at {path}");
        return sprite;
    }

    // The icon as a crisp 16-pixels-per-unit sprite; also tries the old misspelt bouquet file name
    private static Sprite LoadIcon(string relativePath)
    {
        string path = IconFolder + relativePath;
        if (AssetDatabase.LoadAssetAtPath<Texture2D>(path) == null && relativePath.Contains("Bouquet"))
            path = IconFolder + relativePath.Replace("Bouquet", "Bouque");

        if (AssetImporter.GetAtPath(path) is TextureImporter importer &&
            (importer.textureType != TextureImporterType.Sprite || importer.filterMode != FilterMode.Point))
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
        }

        var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        if (sprite == null)
            Debug.LogWarning($"Shop: no icon at {path}");
        return sprite;
    }

    private static void CreateWindow(CurrencyUI currency)
    {
        Canvas canvas = currency.GetComponentInParent<Canvas>(true);
        Transform parent = canvas != null ? canvas.rootCanvas.transform : currency.transform;
        if (parent.Find("Shop") != null)
        {
            Debug.Log("Shop: the Shop window already exists; delete it first to rebuild it.");
            return;
        }

        TMP_FontAsset titleFont = UILayoutSetup.GetFontAsset(UILayoutSetup.TitleFontPath);
        TMP_FontAsset textFont = UILayoutSetup.GetFontAsset(UILayoutSetup.TextFontPath);
        Sprite board = UILayoutSetup.LoadSprite(UILayoutSetup.BoardPath);
        Sprite grassIcon = AssetDatabase.LoadAllAssetsAtPath(UILayoutSetup.GrassSheetPath).OfType<Sprite>()
                                        .FirstOrDefault(s => s.name == UILayoutSetup.GrassIconName);
        float referencePixelsPerUnit = canvas != null ? canvas.referencePixelsPerUnit : 100f;

        // Root stays switched on (so the Shop script is found); the window under it shows while open,
        // just under the grass counter so the grass stays visible on top of the dimmed background
        RectTransform root = UILayoutSetup.CreateRect("Shop", parent);
        UILayoutSetup.Stretch(root, Vector2.zero, Vector2.zero);
        Transform counter = parent.Find("GrassCounter");
        if (counter != null)
            root.SetSiblingIndex(counter.GetSiblingIndex());

        RectTransform window = UILayoutSetup.CreateRect("Window", root);
        UILayoutSetup.Stretch(window, Vector2.zero, Vector2.zero);

        Image dim = UILayoutSetup.CreateImage("Dim", window, null);
        dim.color = new Color(0f, 0f, 0f, 0.65f);
        dim.raycastTarget = true; // blocks clicks to anything behind
        UILayoutSetup.Stretch(dim.rectTransform, Vector2.zero, Vector2.zero);

        RectTransform content = UILayoutSetup.CreateRect("Content", window);
        UILayoutSetup.Stretch(content, Vector2.zero, Vector2.zero);

        TextMeshProUGUI title = UILayoutSetup.CreateText("Title", content, "SHOP", titleFont, 110f);
        title.color = LightText;
        Place(title.rectTransform, new Vector2(0f, 390f), new Vector2(900f, 150f));
        Undo.AddComponent<WavyText>(title.gameObject);

        var shop = Undo.AddComponent<Shop>(root.gameObject);
        var serialized = new SerializedObject(shop);
        serialized.FindProperty("window").objectReferenceValue = window;
        serialized.FindProperty("content").objectReferenceValue = content;

        // Cards
        const int cardCount = 3;
        var cardSize = new Vector2(84f, 112f) * UIPixels.Scale;
        const float cardSpacing = 60f;
        SerializedProperty cards = serialized.FindProperty("cards");
        cards.arraySize = cardCount;
        for (int i = 0; i < cardCount; i++)
        {
            float x = (i - (cardCount - 1) * 0.5f) * (cardSize.x + cardSpacing);
            Button button = CreateButton("Card " + (i + 1), content, board, referencePixelsPerUnit, new Vector2(x, 20f), cardSize);
            RectTransform card = (RectTransform)button.transform;

            TextMeshProUGUI cardName = UILayoutSetup.CreateText("Name", card, "Upgrade", textFont, 32f);
            cardName.textWrappingMode = TextWrappingModes.Normal;
            PlaceFromTop(cardName.rectTransform, 34f, new Vector2(cardSize.x - 50f, 80f));

            Image icon = UILayoutSetup.CreateImage("Icon", card, null);
            icon.preserveAspect = true;
            PlaceFromTop(icon.rectTransform, 120f, Vector2.one * 16f * UIPixels.Scale);

            TextMeshProUGUI level = UILayoutSetup.CreateText("Level", card, "Lv 0 → 1", textFont, 28f);
            PlaceFromTop(level.rectTransform, 200f, new Vector2(cardSize.x - 50f, 36f));

            TextMeshProUGUI description = UILayoutSetup.CreateText("Description", card, "What it does", textFont, 24f);
            description.textWrappingMode = TextWrappingModes.Normal;
            PlaceFromTop(description.rectTransform, 244f, new Vector2(cardSize.x - 56f, 110f));

            TextMeshProUGUI price = CreatePrice(card, grassIcon, textFont);

            SerializedProperty view = cards.GetArrayElementAtIndex(i);
            view.FindPropertyRelative("button").objectReferenceValue = button;
            view.FindPropertyRelative("icon").objectReferenceValue = icon;
            view.FindPropertyRelative("nameText").objectReferenceValue = cardName;
            view.FindPropertyRelative("descriptionText").objectReferenceValue = description;
            view.FindPropertyRelative("levelText").objectReferenceValue = level;
            view.FindPropertyRelative("priceText").objectReferenceValue = price;
        }

        // Reroll and skip
        var buttonSize = new Vector2(70f, 22f) * UIPixels.Scale;
        Button reroll = CreateButton("Reroll", content, board, referencePixelsPerUnit, new Vector2(-170f, -330f), buttonSize);
        TextMeshProUGUI rerollText = UILayoutSetup.CreateText("Label", reroll.transform, "Reroll 10", textFont, 32f);
        UILayoutSetup.Stretch(rerollText.rectTransform, new Vector2(20f, 0f), new Vector2(-64f, 0f));
        Image rerollGrass = UILayoutSetup.CreateImage("Grass", reroll.transform, grassIcon);
        rerollGrass.preserveAspect = true;
        RectTransform rerollGrassRect = rerollGrass.rectTransform;
        rerollGrassRect.anchorMin = rerollGrassRect.anchorMax = rerollGrassRect.pivot = new Vector2(1f, 0.5f);
        rerollGrassRect.anchoredPosition = new Vector2(-28f, 0f);
        UIPixels.Fit(rerollGrass);

        Button skip = CreateButton("Skip", content, board, referencePixelsPerUnit, new Vector2(170f, -330f), buttonSize);
        TextMeshProUGUI skipText = UILayoutSetup.CreateText("Label", skip.transform, "Leave", textFont, 32f);
        UILayoutSetup.Stretch(skipText.rectTransform, Vector2.zero, Vector2.zero);

        serialized.FindProperty("rerollButton").objectReferenceValue = reroll;
        serialized.FindProperty("rerollText").objectReferenceValue = rerollText;
        serialized.FindProperty("skipButton").objectReferenceValue = skip;
        serialized.ApplyModifiedProperties();

        window.gameObject.SetActive(false);
    }

    // The Totem's icon in the top-right corner, with no frame (a dim copy under a bright radial fill that
    // shows the channel), and a framed slot for a seed just right of the weapon bar
    private static void CreateHeldItems(CurrencyUI currency)
    {
        Canvas canvas = currency.GetComponentInParent<Canvas>(true);
        Transform parent = canvas != null ? canvas.rootCanvas.transform : currency.transform;
        if (parent.Find("HeldItems") != null)
        {
            Debug.Log("Shop: HeldItems already exists; delete it first to rebuild it.");
            return;
        }

        const float pixelScale = UIPixels.Scale;
        const float slotSize = 24f * pixelScale;
        const float totemSize = 16f * pixelScale;
        Sprite board = UILayoutSetup.LoadSprite(UILayoutSetup.BoardPath);
        float referencePixelsPerUnit = canvas != null ? canvas.referencePixelsPerUnit : 100f;
        var settings = AssetDatabase.LoadAssetAtPath<ShopSettings>(SettingsPath);
        Sprite totemIcon = settings != null ? settings.Get(UpgradeType.Totem)?.icon : null;
        Sprite seedIcon = settings != null ? settings.Get(UpgradeType.CrimsonWeed)?.icon : null;

        // Covers the canvas, so each icon can be anchored to its own corner
        RectTransform held = UILayoutSetup.CreateRect("HeldItems", parent);
        UILayoutSetup.Stretch(held, Vector2.zero, Vector2.zero);

        RectTransform totem = UILayoutSetup.CreateRect("Totem", held);
        totem.anchorMin = totem.anchorMax = totem.pivot = Vector2.one;
        totem.anchoredPosition = new Vector2(-24f, -24f);
        totem.sizeDelta = Vector2.one * totemSize;

        Image dim = UILayoutSetup.CreateImage("Dim", totem, totemIcon);
        dim.color = new Color(1f, 1f, 1f, 0.3f);
        dim.preserveAspect = true;
        UILayoutSetup.Stretch(dim.rectTransform, Vector2.zero, Vector2.zero);

        Image charge = UILayoutSetup.CreateImage("Icon", totem, totemIcon);
        charge.preserveAspect = true;
        charge.type = Image.Type.Filled;
        charge.fillMethod = Image.FillMethod.Radial360;
        charge.fillOrigin = (int)Image.Origin360.Top;
        charge.fillClockwise = true;
        charge.fillAmount = 1f;
        UILayoutSetup.Stretch(charge.rectTransform, Vector2.zero, Vector2.zero);

        RectTransform seed = CreateHeldSlot("Seed", held, board, referencePixelsPerUnit, pixelScale, slotSize, seedIcon);
        seed.anchorMin = seed.anchorMax = new Vector2(0.5f, 0f);
        seed.anchoredPosition = new Vector2(240f + slotSize * 0.5f, 24f + slotSize * 0.5f); // past the weapon bar's right end

        var heldUI = Undo.AddComponent<HeldItemsUI>(held.gameObject);
        var serialized = new SerializedObject(heldUI);
        serialized.FindProperty("totemIcon").objectReferenceValue = totem;
        serialized.FindProperty("totemCharge").objectReferenceValue = charge;
        serialized.FindProperty("seedIcon").objectReferenceValue = seed;
        serialized.ApplyModifiedProperties();
    }

    private static RectTransform CreateHeldSlot(string name, Transform parent, Sprite board, float referencePixelsPerUnit,
                                                float pixelScale, float size, Sprite icon)
    {
        Image frame = UILayoutSetup.CreateImage(name, parent, board);
        if (board != null && board.border != Vector4.zero)
        {
            frame.type = Image.Type.Sliced;
            frame.pixelsPerUnitMultiplier = referencePixelsPerUnit / (board.pixelsPerUnit * pixelScale);
        }
        RectTransform slot = frame.rectTransform;
        slot.pivot = new Vector2(0.5f, 0.5f);
        slot.sizeDelta = Vector2.one * size;

        Image image = UILayoutSetup.CreateImage("Icon", slot, icon);
        image.preserveAspect = true;
        image.rectTransform.anchorMin = image.rectTransform.anchorMax = image.rectTransform.pivot = new Vector2(0.5f, 0.5f);
        UIPixels.Fit(image);
        return slot;
    }

    // A board (sliced, at the shop's pixel size) that works as a button, tinted when picked
    private static Button CreateButton(string name, Transform parent, Sprite board, float referencePixelsPerUnit,
                                       Vector2 position, Vector2 size)
    {
        Image image = UILayoutSetup.CreateImage(name, parent, board);
        image.raycastTarget = true;
        if (board != null && board.border != Vector4.zero)
        {
            image.type = Image.Type.Sliced;
            image.pixelsPerUnitMultiplier = referencePixelsPerUnit / (board.pixelsPerUnit * PixelScale);
        }
        Place(image.rectTransform, position, size);

        var button = image.gameObject.AddComponent<Button>();
        button.targetGraphic = image;
        ColorBlock colors = button.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1f, 0.93f, 0.7f);
        colors.selectedColor = new Color(1f, 0.93f, 0.7f);
        colors.pressedColor = new Color(0.85f, 0.8f, 0.65f);
        colors.disabledColor = new Color(0.55f, 0.55f, 0.55f);
        button.colors = colors;
        return button;
    }

    // Grass icon and price along the bottom of a card
    private static TextMeshProUGUI CreatePrice(RectTransform card, Sprite grassIcon, TMP_FontAsset font)
    {
        RectTransform row = UILayoutSetup.CreateRect("Price", card);
        row.anchorMin = row.anchorMax = row.pivot = new Vector2(0.5f, 0f);
        row.anchoredPosition = new Vector2(0f, 20f);
        row.sizeDelta = new Vector2(180f, 16f * UIPixels.Scale);

        Image icon = UILayoutSetup.CreateImage("Grass", row, grassIcon);
        icon.preserveAspect = true;
        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        UIPixels.Fit(icon);

        TextMeshProUGUI price = UILayoutSetup.CreateText("Amount", row, "0", font, 36f);
        price.alignment = TextAlignmentOptions.Left;
        UILayoutSetup.Stretch(price.rectTransform, new Vector2(iconRect.sizeDelta.x + 8f, 0f), Vector2.zero);
        return price;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void PlaceFromTop(RectTransform rect, float fromTop, Vector2 size)
    {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
        rect.anchoredPosition = new Vector2(0f, -fromTop);
        rect.sizeDelta = size;
    }
}
