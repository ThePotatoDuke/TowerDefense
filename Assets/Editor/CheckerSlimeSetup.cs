using System.Linq;
using UnityEditor;
using UnityEngine;

// Tools > Set Up Checker Slimes: makes what the Checker Slimes need from the black and white slime sheets
// (sliced like the other slime sheets): an idle animation and an override of the slimes' base controller
// for each colour, the data (its slime numbers copied from the Basic Slime's to start with) and the prefab
// (the Basic Slime's, with the Checker Slime script in place of the slime one). Anything already made is
// kept as it is, so it can be edited freely; delete one to remake it. Add the prefab to a wave in
// Resources/WaveSettings: each of its Base Count spawns a whole group.
public static class CheckerSlimeSetup
{
    private const string SheetFolder = "Assets/3rd Party/Sprites/Enemies/Slimes";
    private const string ClipFolder = "Assets/Animations/Enemies/Slime/Clips";
    private const string ControllerFolder = "Assets/Animations/Enemies/Slime/Controllers";
    private const string SourceClip = ClipFolder + "/Slime_Idle_Red.anim";
    private const string SourceController = ControllerFolder + "/Slime_Override_Red.overrideController";
    private const string SourcePrefab = "Assets/Prefabs/Enemies/Slimes/BasicSlime.prefab";
    private const string DataPath = "Assets/ScriptableObjects/Enemies/Slimes/CheckerSlimeSO.asset";
    private const string PrefabPath = "Assets/Prefabs/Enemies/Slimes/CheckerSlime.prefab";

    [MenuItem("Tools/Set Up Checker Slimes")]
    private static void SetUp()
    {
        RuntimeAnimatorController black = ControllerFor("Black");
        RuntimeAnimatorController white = ControllerFor("White");
        if (black == null || white == null) return;

        CheckerSlimeDataSO data = DataFor(black, white);
        GameObject prefab = PrefabFor(data, black);
        AssetDatabase.SaveAssets();

        if (prefab != null)
        {
            Debug.Log($"Checker Slimes: ready at {PrefabPath} (data at {DataPath}). Add it to a wave in " +
                      "Resources/WaveSettings; each of its Base Count is a whole group.", prefab);
            EditorGUIUtility.PingObject(prefab);
        }
    }

    // The colour's override controller, playing its idle clip in place of the base one
    private static RuntimeAnimatorController ControllerFor(string colour)
    {
        string controllerPath = $"{ControllerFolder}/Slime_Override_{colour}.overrideController";
        var existing = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(controllerPath);
        if (existing != null) return existing;

        string clipPath = $"{ClipFolder}/Slime_Idle_{colour}.anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);
        if (clip == null)
        {
            string sheet = $"{SheetFolder}/{colour}Slime.png";
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(sheet).OfType<Sprite>().ToArray();
            if (sprites.Length == 0)
            {
                Debug.LogWarning($"Checker Slimes: {sheet} has no sprites; slice it like the other slime sheets.");
                return null;
            }
            if (!AssetDatabase.CopyAsset(SourceClip, clipPath))
            {
                Debug.LogWarning($"Checker Slimes: couldn't copy {SourceClip}.");
                return null;
            }
            clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(clipPath);

            // The same frames from this colour's sheet (sprites named the same but for the colour)
            foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
            {
                ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
                for (int i = 0; i < keys.Length; i++)
                {
                    if (keys[i].value is not Sprite frame) continue;
                    string number = frame.name.Substring(frame.name.LastIndexOf('_'));
                    Sprite match = sprites.FirstOrDefault(sprite => sprite.name == $"{colour}Slime{number}");
                    if (match != null)
                        keys[i].value = match;
                }
                AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);
            }
            EditorUtility.SetDirty(clip);
        }

        if (!AssetDatabase.CopyAsset(SourceController, controllerPath))
        {
            Debug.LogWarning($"Checker Slimes: couldn't copy {SourceController}.");
            return null;
        }
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorOverrideController>(controllerPath);
        var overrides = new System.Collections.Generic.List<System.Collections.Generic.KeyValuePair<AnimationClip, AnimationClip>>();
        controller.GetOverrides(overrides);
        for (int i = 0; i < overrides.Count; i++)
            overrides[i] = new(overrides[i].Key, clip);
        controller.ApplyOverrides(overrides);
        EditorUtility.SetDirty(controller);
        return controller;
    }

    private static CheckerSlimeDataSO DataFor(RuntimeAnimatorController black, RuntimeAnimatorController white)
    {
        var data = AssetDatabase.LoadAssetAtPath<CheckerSlimeDataSO>(DataPath);
        if (data != null) return data;

        data = ScriptableObject.CreateInstance<CheckerSlimeDataSO>();
        // Health, damage and squash and stretch like a Basic Slime's (the fields with the same names)
        var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefab)?.GetComponent<SlimeEnemy>();
        if (source != null && source.Data != null)
            JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(source.Data), data);
        data.enemyName = "Checker Slime";
        data.dropsFromSky = false;
        data.blackAnimator = black;
        data.whiteAnimator = white;
        AssetDatabase.CreateAsset(data, DataPath);
        return data;
    }

    private static GameObject PrefabFor(CheckerSlimeDataSO data, RuntimeAnimatorController black)
    {
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        if (existing != null) return existing;

        var source = AssetDatabase.LoadAssetAtPath<GameObject>(SourcePrefab);
        if (source == null)
        {
            Debug.LogWarning($"Checker Slimes: {SourcePrefab} not found.");
            return null;
        }

        var root = (GameObject)PrefabUtility.InstantiatePrefab(source);
        PrefabUtility.UnpackPrefabInstance(root, PrefabUnpackMode.OutermostRoot, InteractionMode.AutomatedAction);
        root.name = "CheckerSlime";

        var slime = root.GetComponent<SlimeEnemy>();
        Transform slimeChild = slime != null
            ? new SerializedObject(slime).FindProperty("slimeChild").objectReferenceValue as Transform
            : null;
        if (slime != null)
            Object.DestroyImmediate(slime);

        var checker = root.AddComponent<CheckerSlimeEnemy>();
        var serialized = new SerializedObject(checker);
        serialized.FindProperty("data").objectReferenceValue = data;
        serialized.FindProperty("slimeChild").objectReferenceValue = slimeChild;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        var animator = root.GetComponentInChildren<Animator>(true);
        if (animator != null)
            animator.runtimeAnimatorController = black;

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        Object.DestroyImmediate(root);
        return saved;
    }
}
