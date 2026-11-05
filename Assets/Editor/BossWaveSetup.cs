using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

// Tools > Set Up Boss Waves: makes the Slime Queen (her idle animation from the sliced slime_queen sheet,
// an override of the slimes' base controller, her data and her prefab: the Basic Slime's, with the queen's
// script and her body, hitbox, health bar and Tibbit seat fitted to her bigger sprite), then fills
// Resources/WaveSettings' boss waves with a starting set if it has none, and gives the scene's Music the
// boss track if it has none. Anything already made is kept as it is, so it can be edited freely; delete
// one to remake it.
public static class BossWaveSetup
{
    private const string SheetPath = "Assets/3rd Party/Sprites/Enemies/Slimes/slime_queen.png";
    private const string SourceClip = "Assets/Animations/Enemies/Slime/Clips/Slime_Idle_Red.anim";
    private const string ClipPath = "Assets/Animations/Enemies/Slime/Clips/Slime_Idle_Queen.anim";
    private const string SourceController = "Assets/Animations/Enemies/Slime/Controllers/Slime_Override_Red.overrideController";
    private const string ControllerPath = "Assets/Animations/Enemies/Slime/Controllers/Slime_Override_Queen.overrideController";
    private const string SourceData = "Assets/ScriptableObjects/Enemies/Slimes/HeavySlimeSO.asset";
    private const string DataPath = "Assets/ScriptableObjects/Enemies/Slimes/SlimeQueenSO.asset";
    private const string SourcePrefab = "Assets/Prefabs/Enemies/Slimes/BasicSlime.prefab";
    private const string PrefabPath = "Assets/Prefabs/Enemies/Slimes/SlimeQueen.prefab";
    private const string BossMusicPath = "Assets/Audio/Music/special_wave.ogg";
    private const string Enemies = "Assets/Prefabs/Enemies/";
    private const float FramesPerSecond = 8f;

    [MenuItem("Tools/Set Up Boss Waves")]
    private static void SetUp()
    {
        AnimatorOverrideController controller = QueenController();
        SlimeQueenDataSO data = QueenData();
        SlimeQueenEnemy queen = controller != null && data != null ? QueenPrefab(data, controller) : null;
        AssetDatabase.SaveAssets();

        FillBossWaves(queen);
        GiveMusicBossTrack();

        if (queen != null)
            EditorGUIUtility.PingObject(queen.gameObject);
        Debug.Log("Boss waves: ready. They're in Resources/WaveSettings under Boss Waves; the Slime Queen's " +
                  $"prefab is at {PrefabPath} and her data at {DataPath}. In play, B makes the next wave a boss wave.");
    }

    // Her idle: the sheet's frames in order, looping
    private static AnimatorOverrideController QueenController()
    {
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ControllerPath);
        if (existing != null) return existing;

        Sprite[] frames = AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>()
            .OrderBy(sprite => int.Parse(sprite.name.Substring(sprite.name.LastIndexOf('_') + 1))).ToArray();
        if (frames.Length == 0)
        {
            Debug.LogWarning($"Boss waves: {SheetPath} has no sprites; slice it like the other slime sheets.");
            return null;
        }

        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (clip == null)
        {
            if (!AssetDatabase.CopyAsset(SourceClip, ClipPath))
            {
                Debug.LogWarning($"Boss waves: couldn't copy {SourceClip}.");
                return null;
            }
            clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
            clip.frameRate = FramesPerSecond;
            foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                // Each frame for one step, and the last held to the end of the loop
                var keys = new ObjectReferenceKeyframe[frames.Length + 1];
                for (int i = 0; i <= frames.Length; i++)
                    keys[i] = new ObjectReferenceKeyframe
                    {
                        time = i / FramesPerSecond,
                        value = frames[Mathf.Min(i, frames.Length - 1)]
                    };
                AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            }
            EditorUtility.SetDirty(clip);
        }

        if (!AssetDatabase.CopyAsset(SourceController, ControllerPath))
        {
            Debug.LogWarning($"Boss waves: couldn't copy {SourceController}.");
            return null;
        }
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(ControllerPath);
        var overrides = new List<KeyValuePair<AnimationClip, AnimationClip>>();
        controller.GetOverrides(overrides);
        for (int i = 0; i < overrides.Count; i++)
            overrides[i] = new(overrides[i].Key, clip);
        controller.ApplyOverrides(overrides);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    // A Heavy Slime's numbers to start with, made much tougher and slower, and her brood
    private static SlimeQueenDataSO QueenData()
    {
        var data = AssetDatabase.LoadAssetAtPath<SlimeQueenDataSO>(DataPath);
        if (data != null) return data;

        data = ScriptableObject.CreateInstance<SlimeQueenDataSO>();
        var source = AssetDatabase.LoadAssetAtPath<SlimeDataSO>(SourceData);
        if (source != null)
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source), data);
        data.enemyName = "Slime Queen";
        data.dropsFromSky = false;
        data.maxHealth = 1500f;
        data.contactDamage = 5f;
        data.bounceHeight = 1.2f;
        data.bounceDuration = 0.6f;
        data.bounceInterval = 0.9f;
        data.baseHopDistance = 1.5f;
        data.hopPattern = SlimeDataSO.HopPattern.Regular;
        data.brood = new[] { Enemy("Slimes/BasicSlime"), Enemy("Slimes/ChargerSlime"), Enemy("Slimes/MiniSlime") }
            .Where(enemy => enemy != null).ToArray();
        AssetDatabase.CreateAsset(data, DataPath);
        return data;
    }

    private static SlimeQueenEnemy QueenPrefab(SlimeQueenDataSO data, RuntimeAnimatorController controller)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null) return existing.GetComponent<SlimeQueenEnemy>();

        var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefab);
        Sprite firstFrame = AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>()
            .FirstOrDefault(sprite => sprite.name.EndsWith("_0"));
        if (source == null || firstFrame == null)
        {
            Debug.LogWarning($"Boss waves: {SourcePrefab} or the queen's sprites not found.");
            return null;
        }

        var root = (GameObject)PrefabUtility.InstantiatePrefab(source);
        PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
        root.name = "SlimeQueen";
        root.transform.position = Vector3.zero;

        var slime = root.GetComponent<SlimeEnemy>();
        Transform slimeChild = slime != null
            ? new SerializedObject(slime).FindProperty("slimeChild").objectReferenceValue as Transform
            : null;
        if (slime != null)
            Object.DestroyImmediate(slime);

        var queen = root.AddComponent<SlimeQueenEnemy>();
        var serialized = new SerializedObject(queen);
        serialized.FindProperty("data").objectReferenceValue = data;
        serialized.FindProperty("slimeChild").objectReferenceValue = slimeChild;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        var animator = root.GetComponentInChildren<Animator>(true);
        if (animator != null)
            animator.runtimeAnimatorController = controller;

        FitToSprite(root, firstFrame);

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return saved != null ? saved.GetComponent<SlimeQueenEnemy>() : null;
    }

    // Swaps in her sprite (same pixel size: same import settings and scale as the other slimes), standing
    // on the ground where the slime stood, with the hitbox, health bar and Tibbit seat fitted to her
    private static void FitToSprite(GameObject root, Sprite sprite)
    {
        SpriteRenderer body = root.GetComponentsInChildren<SpriteRenderer>(true)
            .FirstOrDefault(renderer => renderer.GetComponent<Animator>() != null)
            ?? root.GetComponentsInChildren<SpriteRenderer>(true).FirstOrDefault(renderer => renderer.GetComponentInParent<HealthBar>(true) == null);
        if (body == null) return;

        Bounds before = body.bounds;
        body.sprite = sprite;
        Bounds after = body.bounds;
        // Feet back on the ground
        body.transform.position += Vector3.up * (before.min.y - after.min.y);
        after = body.bounds;
        float grow = after.size.x / Mathf.Max(0.001f, before.size.x);
        float rise = after.max.y - before.max.y;

        // The hitbox: as much bigger as she's wider, round her middle
        var collider = body.GetComponentInParent<SphereCollider>(true) ?? root.GetComponentInChildren<SphereCollider>(true);
        if (collider != null)
        {
            collider.radius *= grow;
            Vector3 center = collider.transform.InverseTransformPoint(after.center);
            collider.center = new Vector3(collider.center.x, center.y, collider.center.z);
        }

        // Things over her head go up to the top of her
        foreach (HealthBar bar in root.GetComponentsInChildren<HealthBar>(true))
            bar.transform.position += Vector3.up * rise;
        foreach (TibbitPerch perch in root.GetComponentsInChildren<TibbitPerch>(true))
            perch.transform.position += Vector3.up * rise;
    }

    private static EnemyBase Enemy(string path) =>
        AssetDatabase.LoadAssetAtPath<GameObject>(Enemies + path + ".prefab")?.GetComponent<EnemyBase>();

    private static EnemySpawnEntry Entry(string path, int count, bool inARow = false, float rowGap = 0.35f,
                                         int groupSize = 0, string partner = null) =>
        new()
        {
            enemyPrefab = Enemy(path),
            spawnWeight = 0f,
            baseCount = count,
            inARow = inARow,
            rowGap = rowGap,
            groupSize = groupSize,
            partner = partner != null ? Enemy(partner) : null,
            partnerGap = 0.4f
        };

    private static BossWave Boss(string title, float gapMin, float gapMax, params EnemySpawnEntry[] enemies) =>
        new()
        {
            name = title,
            bannerTitle = title,
            bannerSubtitle = "BOSS WAVE",
            enemies = enemies.Where(entry => entry.enemyPrefab != null).ToList(),
            spawnIntervalMin = gapMin,
            spawnIntervalMax = gapMax,
            burstChance = 0f
        };

    private static void FillBossWaves(SlimeQueenEnemy queen)
    {
        WaveSettings settings = WaveSettings.Instance;
        if (settings == null)
        {
            Debug.LogWarning("Boss waves: no Resources/WaveSettings.");
            return;
        }
        if (settings.bossWaves.Count > 0) return; // already set up: keep them as they are

        Undo.RecordObject(settings, "Set Up Boss Waves");
        var waves = settings.bossWaves;

        waves.Add(Boss("Konga Express!", 1.5f, 2.5f,
            Entry("Konga/Konga", 30, inARow: true, rowGap: 0.3f),
            Entry("Rollers/Roller", 4)));

        waves.Add(Boss("The Horde!", 3f, 5f,
            Entry("Slimes/MiniSlime", 15, inARow: true, rowGap: 0.12f),
            Entry("Slimes/MiniSlime", 15, inARow: true, rowGap: 0.12f),
            Entry("Slimes/MiniSlime", 15, inARow: true, rowGap: 0.12f),
            Entry("Slimes/MiniSlime", 15, inARow: true, rowGap: 0.12f)));

        waves.Add(Boss("Air Raid!", 2f, 3f,
            Entry("Frills/GreenFrills", 4, partner: "Tibbits/Tibbit"),
            Entry("Frills/PinkFrills", 3, partner: "Tibbits/Tibbit"),
            Entry("Frills/WhiteFrills", 2, partner: "Tibbits/OrangeTibbit")));

        waves.Add(Boss("Checkmate!", 3f, 4f,
            Entry("Slimes/CheckerSlime", 5, groupSize: 8)));

        waves.Add(Boss("Stampede!", 0.15f, 0.35f,
            Entry("Walkers/HyperWalker", 12),
            Entry("Slimes/HyperSlime", 12),
            Entry("Rollers/Roller", 3),
            Entry("SpikeBalls/SpikeBallRed", 2),
            Entry("SpikeBalls/SpikeBallBlack", 2)));

        waves.Add(Boss("Bomb Squad!", 1.5f, 2.5f,
            Entry("Bomberrs/Bomberr", 10)));

        BossWave batCave = Boss("Bat Cave!", 0.5f, 1f,
            Entry("Bats/BasicBat", 10),
            Entry("Bats/RedBat", 5),
            Entry("Bats/PurpleBat", 5));
        batCave.dark = true;
        waves.Add(batCave);

        BossWave spikeRain = Boss("Spike Rain!", 0.8f, 1.5f,
            Entry("Walkers/BasicWalker", 10),
            Entry("Walkers/HeavyWalker", 3),
            Entry("Slimes/BasicSlime", 8));
        spikeRain.spikeRain = Enemy("SpikeBalls/SpikeBallYellow") as SpikeBallEnemy;
        waves.Add(spikeRain);

        if (queen != null)
        {
            BossWave royal = Boss("The Slime Queen!", 1f, 2f,
                Entry("Slimes/BasicSlime", 6));
            royal.enemies.Insert(0, new EnemySpawnEntry { enemyPrefab = queen, spawnWeight = 0f, baseCount = 1 });
            royal.growthPerRepeat = 0f; // one queen, however many times she comes
            waves.Add(royal);
        }

        EditorUtility.SetDirty(settings);
        AssetDatabase.SaveAssets();
    }

    private static void GiveMusicBossTrack()
    {
        var music = Object.FindAnyObjectByType<MusicManager>(FindObjectsInactive.Include);
        var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(BossMusicPath);
        if (music == null || clip == null) return;

        var serialized = new SerializedObject(music);
        SerializedProperty boss = serialized.FindProperty("specialWave");
        if (boss.objectReferenceValue != null) return;
        boss.objectReferenceValue = clip;
        serialized.ApplyModifiedProperties();
        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(music.gameObject.scene);
    }
}
