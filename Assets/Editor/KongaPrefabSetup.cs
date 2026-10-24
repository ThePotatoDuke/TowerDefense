using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

// One-shot setup for the Konga enemy: imports its frames as pixel-art sprites, builds a walk animation
// and controller, copies BasicWalker into a Konga prefab with the KongaEnemy script, and creates its
// data asset. Runs by itself after a recompile while the prefab doesn't exist, or from
// Tools > Setup Konga Prefab. Safe to delete once the prefab is set up.
[InitializeOnLoad]
public static class KongaPrefabSetup
{
    private const string SpriteFolder = "Assets/3rd Party/Sprites/Enemies/Konga";
    private static readonly string[] FrameNames = { "konga", "konga2", "konga3", "konga4" };
    private const float FramesPerSecond = 6f;

    private const string AnimationFolder = "Assets/Animations/Enemies/Konga";
    private const string ClipPath = AnimationFolder + "/Konga_Walk.anim";
    private const string ControllerPath = AnimationFolder + "/Konga_Base.controller";

    private const string SourcePrefabPath = "Assets/Prefabs/Enemies/Walkers/BasicWalker.prefab";
    private const string PrefabFolder = "Assets/Prefabs/Enemies/Konga";
    private const string PrefabPath = PrefabFolder + "/Konga.prefab";

    private const string DataFolder = "Assets/ScriptableObjects/Enemies/Konga";
    private const string DataPath = DataFolder + "/KongaSO.asset";

    // Weaving variant: snakes from side to side and only chains with its own kind
    private const string WeaverPrefabPath = PrefabFolder + "/WeaverKonga.prefab";
    private const string WeaverDataPath = DataFolder + "/WeaverKongaSO.asset";
    private static readonly Color WeaverTint = new Color(0.55f, 0.5f, 0.75f, 1f);

    static KongaPrefabSetup()
    {
        EditorApplication.delayCall += () =>
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null)
                Run();
            if (AssetDatabase.LoadAssetAtPath<GameObject>(WeaverPrefabPath) == null)
                SetupWeaver();
        };
    }

    [MenuItem("Tools/Setup Weaver Konga Prefab")]
    public static void SetupWeaver()
    {
        KongaDataSO baseData = AssetDatabase.LoadAssetAtPath<KongaDataSO>(DataPath);
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null || baseData == null)
        {
            Debug.LogError("Weaver Konga setup: set up the Konga first (Tools > Setup Konga Prefab).");
            return;
        }

        KongaDataSO data = AssetDatabase.LoadAssetAtPath<KongaDataSO>(WeaverDataPath);
        if (data == null)
        {
            data = Object.Instantiate(baseData);
            data.enemyName = "WeaverKonga";
            data.waveAmplitude = 1f;
            data.waveLength = 4f;
            AssetDatabase.CreateAsset(data, WeaverDataPath);
            AssetDatabase.SaveAssets();
        }

        if (AssetDatabase.LoadAssetAtPath<GameObject>(WeaverPrefabPath) == null && !AssetDatabase.CopyAsset(PrefabPath, WeaverPrefabPath))
        {
            Debug.LogError($"Weaver Konga setup: couldn't copy {PrefabPath}");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(WeaverPrefabPath);
        try
        {
            root.name = "WeaverKonga";

            var serialized = new SerializedObject(root.GetComponent<KongaEnemy>());
            serialized.FindProperty("data").objectReferenceValue = data;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            SpriteRenderer spriteRenderer = root.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
                spriteRenderer.color = WeaverTint;

            PrefabUtility.SaveAsPrefabAsset(root, WeaverPrefabPath);
            Debug.Log($"Weaver Konga setup: created {WeaverPrefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    [MenuItem("Tools/Setup Konga Prefab")]
    public static void Run()
    {
        Sprite[] frames = ImportFrames();
        if (frames == null) return;

        RuntimeAnimatorController controller = GetOrCreateController(frames);
        KongaDataSO data = GetOrCreateData();

        EnsureFolder(PrefabFolder);
        bool isNew = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) == null;
        if (isNew && !AssetDatabase.CopyAsset(SourcePrefabPath, PrefabPath))
        {
            Debug.LogError($"Konga setup: couldn't copy {SourcePrefabPath}");
            return;
        }

        GameObject root = PrefabUtility.LoadPrefabContents(PrefabPath);
        try
        {
            root.name = "Konga";

            if (root.TryGetComponent(out WalkerEnemy walker))
                Object.DestroyImmediate(walker);
            if (!root.TryGetComponent(out KongaEnemy konga))
                konga = root.AddComponent<KongaEnemy>();
            var serialized = new SerializedObject(konga);
            serialized.FindProperty("data").objectReferenceValue = data;
            serialized.ApplyModifiedPropertiesWithoutUndo();

            SpriteRenderer spriteRenderer = root.GetComponentInChildren<SpriteRenderer>();
            if (spriteRenderer != null)
                spriteRenderer.sprite = frames[0];

            Animator animator = root.GetComponentInChildren<Animator>();
            if (animator != null)
                animator.runtimeAnimatorController = controller;

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Debug.Log($"Konga setup: created {PrefabPath}");
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }

        if (isNew)
            AddToFirstWave(AssetDatabase.LoadAssetAtPath<KongaEnemy>(PrefabPath));
    }

    // For testing the chain: 6 Kongas in the open scene's first wave, spawning close together
    private static void AddToFirstWave(KongaEnemy prefab)
    {
        var manager = Object.FindAnyObjectByType<EndlessWaveManager>();
        if (manager == null || prefab == null)
        {
            Debug.LogWarning("Konga setup: no EndlessWaveManager in the open scene; add the Konga to a wave by hand.");
            return;
        }

        var serialized = new SerializedObject(manager);
        SerializedProperty waves = serialized.FindProperty("waves");
        if (waves.arraySize == 0) return;

        SerializedProperty wave = waves.GetArrayElementAtIndex(0);
        SerializedProperty enemies = wave.FindPropertyRelative("enemies");
        enemies.InsertArrayElementAtIndex(enemies.arraySize);
        SerializedProperty entry = enemies.GetArrayElementAtIndex(enemies.arraySize - 1);
        entry.FindPropertyRelative("enemyPrefab").objectReferenceValue = prefab;
        entry.FindPropertyRelative("spawnWeight").floatValue = 0f;
        entry.FindPropertyRelative("baseCount").intValue = 6;
        wave.FindPropertyRelative("spawnIntervalMin").floatValue = 0.5f;
        wave.FindPropertyRelative("spawnIntervalMax").floatValue = 0.9f;
        serialized.ApplyModifiedProperties();

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(manager.gameObject.scene);
        Debug.Log("Konga setup: added 6 Kongas to the first wave. Save the scene to keep it.");
    }

    // Same import settings as the other enemy sprites: 16 pixels per unit, point filtering, no compression
    private static Sprite[] ImportFrames()
    {
        var frames = new Sprite[FrameNames.Length];
        for (int i = 0; i < FrameNames.Length; i++)
        {
            string path = $"{SpriteFolder}/{FrameNames[i]}.png";
            if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
            {
                Debug.LogError($"Konga setup: no texture at {path}");
                return null;
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 16f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.SaveAndReimport();

            frames[i] = AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }
        return frames;
    }

    private static RuntimeAnimatorController GetOrCreateController(Sprite[] frames)
    {
        EnsureFolder(AnimationFolder);

        AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(ClipPath);
        if (clip == null)
        {
            clip = new AnimationClip { frameRate = FramesPerSecond };

            // Unity already holds the last sprite key for one frame before looping
            var keys = new ObjectReferenceKeyframe[frames.Length];
            for (int i = 0; i < frames.Length; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / FramesPerSecond, value = frames[i] };

            var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = true;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AssetDatabase.CreateAsset(clip, ClipPath);
        }

        RuntimeAnimatorController controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);
        if (controller == null)
            controller = AnimatorController.CreateAnimatorControllerAtPathWithClip(ControllerPath, clip);
        return controller;
    }

    private static KongaDataSO GetOrCreateData()
    {
        KongaDataSO data = AssetDatabase.LoadAssetAtPath<KongaDataSO>(DataPath);
        if (data != null) return data;

        EnsureFolder(DataFolder);
        data = ScriptableObject.CreateInstance<KongaDataSO>();
        data.enemyName = "Konga";
        data.maxHealth = 60f;
        data.contactDamage = 1f;
        AssetDatabase.CreateAsset(data, DataPath);
        AssetDatabase.SaveAssets();
        return data;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        int split = path.LastIndexOf('/');
        EnsureFolder(path.Substring(0, split));
        AssetDatabase.CreateFolder(path.Substring(0, split), path.Substring(split + 1));
    }
}
