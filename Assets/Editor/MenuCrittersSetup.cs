using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tools > Set Up Main Menu Critters: makes a menu critter prefab (Prefabs/Menu/Critters) for each enemy
// prefab that's a single sprite, with its frames (from its Animator's starting animation, or the frames in
// its data for Frills, Tibbits and Mollies) and how it moves (speeds, hop heights and timings, weaving,
// bobbing, from its data), and gives the open main menu a Critters layer over everything that spawns them.
// Prefabs that already exist are kept as they are (delete one to remake it from its enemy), so they can be
// edited freely; only settings added since they were made get filled in. Spike balls (3D models in the
// game) use the menu's own drawn frames (menu_spike1-3), only for the black one; Checker Slimes get both
// colours' frames; rollers and Bomberr are left out.
public static class MenuCrittersSetup
{
    private const string EnemyFolder = "Assets/Prefabs/Enemies";
    private const string CritterFolder = "Assets/Prefabs/Menu/Critters";
    private const float MenuScale = 5f;
    private const string SpikeFrames = "Assets/3rd Party/Sprites/Enemies/Walkers/menu_spike{0}.png";

    [MenuItem("Tools/Set Up Main Menu Critters")]
    private static void SetUp()
    {
        var menu = Object.FindAnyObjectByType<MainMenu>(FindObjectsInactive.Include);
        Canvas canvas = menu != null ? menu.GetComponentInParent<Canvas>() : null;
        if (canvas == null)
        {
            Debug.LogWarning("Menu critters: open the MainMenu scene first (no MainMenu canvas found).");
            return;
        }

        EnsureFolder(CritterFolder);
        var made = new List<string>();
        List<MenuCritter> prefabs = AssetDatabase.FindAssets("t:Prefab", new[] { EnemyFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .OrderBy(path => path)
            .Select(path => AssetDatabase.LoadAssetAtPath<GameObject>(path))
            .Select(enemy => CritterFor(enemy, made))
            .Where(critter => critter != null)
            .ToList();

        Transform existing = canvas.transform.Find("Critters");
        RectTransform layer = existing != null ? (RectTransform)existing : UILayoutSetup.CreateRect("Critters", canvas.transform);
        UILayoutSetup.Stretch(layer, Vector2.zero, Vector2.zero);
        layer.SetAsLastSibling(); // over the sign and buttons

        var spawner = layer.GetComponent<MenuCritters>();
        if (spawner == null)
            spawner = Undo.AddComponent<MenuCritters>(layer.gameObject);
        Undo.RecordObject(spawner, "Set Up Menu Critters");
        spawner.SetCritters(prefabs);
        EditorUtility.SetDirty(spawner);
        EditorSceneManager.MarkSceneDirty(canvas.gameObject.scene);

        Debug.Log($"Menu critters: {prefabs.Count} critters in {CritterFolder}" +
                  (made.Count > 0 ? $" (made or updated: {string.Join(", ", made)})" : " (all already there)") +
                  ". Save the scene to keep them.");
    }

    // The critter prefab for this enemy: the existing one, or a new one made from the enemy
    private static MenuCritter CritterFor(GameObject enemyPrefab, List<string> made)
    {
        if (enemyPrefab == null) return null;
        string path = $"{CritterFolder}/{enemyPrefab.name}.prefab";
        var existing = AssetDatabase.LoadAssetAtPath<MenuCritter>(path);
        bool known = TryReadSettings(enemyPrefab, out MenuCritter.Settings settings);
        if (existing != null)
        {
            if (known && existing.FillMissing(settings))
            {
                EditorUtility.SetDirty(existing);
                AssetDatabase.SaveAssets();
                made.Add(enemyPrefab.name + " (updated)");
            }
            return existing;
        }
        if (!known) return null;

        var root = new GameObject(enemyPrefab.name, typeof(RectTransform));
        var rootRect = (RectTransform)root.transform;
        rootRect.anchorMin = rootRect.anchorMax = new Vector2(0.5f, 0.5f);
        rootRect.sizeDelta = Vector2.zero;

        var bodyObject = new GameObject("Body", typeof(RectTransform), typeof(Image));
        var body = (RectTransform)bodyObject.transform;
        body.SetParent(rootRect, false);
        body.anchorMin = body.anchorMax = new Vector2(0.5f, 0.5f);
        body.pivot = new Vector2(0.5f, 0f); // standing on the spot
        var image = bodyObject.GetComponent<Image>();
        image.raycastTarget = false;
        image.sprite = settings.frames[0];
        body.sizeDelta = settings.frames[0].rect.size * MenuScale;

        root.AddComponent<MenuCritter>().Apply(settings, body, image);
        GameObject saved = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        made.Add(enemyPrefab.name);
        return saved != null ? saved.GetComponent<MenuCritter>() : null;
    }

    private static bool TryReadSettings(GameObject prefab, out MenuCritter.Settings settings)
    {
        settings = new MenuCritter.Settings
        {
            framesPerSecond = 8f, digFramesPerSecond = 10f, peekFramesPerSecond = 12f, speed = 2f,
            hopHeight = 1f, hopDuration = 0.3f, hopInterval = 0.1f, hopDistance = 1f, squash = 0.8f, stretch = 1.2f,
            flyHeight = 0.6f, waveLength = 4f, bobsPerSecond = 1f, idleTime = new Vector2(1.5f, 2.5f),
            groupSize = 1, groupSpacing = 0.9f, weight = 1f,
        };

        var enemy = prefab.GetComponent<EnemyBase>();
        var sprite = prefab.GetComponentInChildren<SpriteRenderer>(true);
        if (enemy == null || enemy.Data == null) return false;

        (settings.frames, settings.framesPerSecond) = AnimatorFrames(prefab);

        switch (enemy.Data)
        {
            case WalkerDataSO walker:
                settings.movement = MenuCritter.Movement.Walk;
                settings.speed = walker.moveSpeed;
                settings.facesLeft = true;
                break;

            case KongaDataSO konga:
                settings.movement = MenuCritter.Movement.Walk;
                settings.speed = konga.moveSpeed;
                settings.groupSize = 4;
                settings.maxGroupSize = 8;
                settings.groupSpacing = konga.spacing;
                settings.weight = 0.6f;
                break;

            case SlimeDataSO slime:
                settings.movement = MenuCritter.Movement.Hop;
                settings.hopHeight = slime.bounceHeight;
                settings.hopDuration = slime.bounceDuration;
                settings.hopInterval = slime.bounceInterval;
                settings.hopDistance = slime.baseHopDistance;
                settings.squash = slime.squash;
                settings.stretch = slime.takeoffStretch;
                settings.hopPattern = (MenuCritter.HopPattern)(int)slime.hopPattern;
                settings.longHopDistance = slime.longHopDistanceMultiplier;
                settings.longHopHeight = slime.longHopHeightMultiplier;
                settings.longHopDuration = slime.longHopDurationMultiplier;
                settings.longHopWait = slime.longHopWaitTime;
                settings.backChance = slime.backChance;
                settings.longForwardChance = slime.longForwardChance;
                settings.minBackForward = slime.minBackForward;
                settings.maxBackForward = slime.maxBackForward;
                settings.speed = slime.baseHopDistance / Mathf.Max(0.05f, slime.bounceDuration + slime.bounceInterval);
                if (prefab.name.Contains("Mini"))
                {
                    settings.groupSize = 6;
                    settings.groupSpacing = 0.4f;
                    settings.weight = 0.6f;
                }
                break;

            case BatDataSO bat:
                settings.movement = MenuCritter.Movement.Fly;
                settings.speed = bat.moveSpeed;
                settings.flyHeight = Mathf.Max(0.6f, bat.hoverHeight);
                settings.waveAmplitude = bat.waveAmplitude;
                settings.waveLength = bat.waveLength;
                settings.bobHeight = bat.bobHeight;
                settings.bobsPerSecond = bat.flapRate;
                settings.facesLeft = true;
                break;

            case FrillsDataSO frills:
                settings.movement = MenuCritter.Movement.Fly;
                settings.speed = frills.startSpeed;
                settings.flyHeight = Mathf.Min(frills.flyHeight, 1.2f);
                settings.bobHeight = frills.bobHeight;
                settings.bobsPerSecond = frills.bobsPerSecond;
                settings.facesLeft = true;
                UseFrames(ref settings, frills.idleFrames, frills.idleFramesPerSecond);
                break;

            case TibbitDataSO tibbit:
                settings.movement = MenuCritter.Movement.Skip;
                settings.speed = tibbit.groundSpeed;
                settings.hopHeight = tibbit.groundHopHeight;
                settings.hopDuration = 1f / Mathf.Max(0.1f, tibbit.groundHopsPerSecond);
                settings.hopDistance = tibbit.groundSpeed * settings.hopDuration;
                settings.airSprite = tibbit.hopSprite; // its jump sprite while skipping and jumping
                settings.facesLeft = true;
                settings.rides = true;
                settings.rideTime = new Vector2(tibbit.minRideTime, tibbit.maxRideTime);
                settings.jumpRange = tibbit.jumpRange;
                settings.jumpHeight = tibbit.jumpHeight;
                settings.jumpSpeed = tibbit.jumpSpeed;
                settings.jumpTime = new Vector2(tibbit.minJumpTime, tibbit.maxJumpTime);
                settings.searchInterval = tibbit.searchInterval;
                UseFrames(ref settings, tibbit.idleFrames, tibbit.idleFramesPerSecond);
                break;

            case MollyDataSO molly:
                settings.movement = MenuCritter.Movement.Dig;
                settings.speed = molly.digSpeed;
                settings.idleTime = new Vector2(molly.minIdleTime, molly.maxIdleTime);
                settings.digFrames = molly.digFrames;
                settings.digFramesPerSecond = molly.digFramesPerSecond;
                settings.peekFrames = molly.peekFrames;
                settings.peekFramesPerSecond = molly.peekFramesPerSecond;
                UseFrames(ref settings, molly.idleFrames, molly.idleFramesPerSecond);
                break;

            case CheckerSlimeDataSO checker:
                settings.movement = MenuCritter.Movement.CheckerHop;
                (Sprite[] black, float blackFps) = ControllerFrames(checker.blackAnimator);
                (settings.altFrames, _) = ControllerFrames(checker.whiteAnimator);
                if (black != null)
                    (settings.frames, settings.framesPerSecond) = (black, blackFps);
                settings.squash = checker.squash;
                settings.stretch = checker.takeoffStretch;
                settings.checkerHopHeight = checker.checkerHopHeight;
                settings.checkerHopHeightPerSlime = checker.checkerHopHeightPerSlime;
                settings.checkerHopGap = checker.checkerHopGap;
                settings.checkerHopAirSpeed = checker.checkerHopAirSpeed;
                settings.groupSize = Mathf.Max(2, checker.minGroupSize);
                settings.maxGroupSize = checker.maxGroupSize;
                settings.groupSpacing = checker.spacing;
                settings.speed = checker.spacing / Mathf.Max(0.05f, checker.checkerHopGap);
                settings.weight = 0.6f;
                break;

            case SpikeBallDataSO spikeBall:
                if (!prefab.name.Contains("Black")) return false; // only the black one has menu frames
                settings.movement = MenuCritter.Movement.Roll;
                settings.speed = spikeBall.maxSpeed;
                settings.frames = Enumerable.Range(1, 3)
                    .Select(i => PixelSprites.Import(string.Format(SpikeFrames, i)))
                    .Where(frame => frame != null).ToArray();
                settings.weight = 0.6f;
                FXSettings fx = AssetDatabase.LoadAssetAtPath<FXSettings>("Assets/Resources/FXSettings.asset");
                if (fx != null)
                {
                    settings.markerOuter = fx.dropMarkerOuter;
                    settings.markerMiddle = fx.dropMarkerMiddle;
                    settings.dropWarningTime = fx.dropWarningTime;
                    settings.markerPopTime = fx.dropMarkerPopTime;
                    settings.markerSpin = fx.dropMarkerSpin;
                    settings.dropFallTime = fx.dropFallTime;
                }
                break;

            default:
                return false; // moves in a way the menu doesn't copy
        }

        if (settings.frames == null || settings.frames.Length == 0)
        {
            if (sprite == null || sprite.sprite == null) return false;
            settings.frames = new[] { sprite.sprite };
        }
        return true;
    }

    private static void UseFrames(ref MenuCritter.Settings settings, Sprite[] frames, float fps)
    {
        if (frames == null || frames.Length == 0) return;
        settings.frames = frames;
        settings.framesPerSecond = fps;
    }

    // The sprites its Animator's starting animation flips through, and how fast
    private static (Sprite[], float) AnimatorFrames(GameObject prefab)
    {
        var animator = prefab.GetComponentInChildren<Animator>(true);
        return ControllerFrames(animator != null ? animator.runtimeAnimatorController : null);
    }

    // The same for a controller (an override plays its own clips in place of the base one's)
    private static (Sprite[], float) ControllerFrames(RuntimeAnimatorController runtime)
    {
        if (runtime == null) return (null, 8f);

        AnimationClip clip = null;
        if (runtime is AnimatorController controller && controller.layers.Length > 0)
            clip = controller.layers[0].stateMachine.defaultState?.motion as AnimationClip;
        if (clip == null)
            clip = runtime.animationClips.FirstOrDefault();
        if (clip == null) return (null, 8f);

        foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(clip))
        {
            if (binding.type != typeof(SpriteRenderer) || binding.propertyName != "m_Sprite") continue;
            Sprite[] frames = AnimationUtility.GetObjectReferenceCurve(clip, binding)
                .Select(key => key.value as Sprite).Where(frame => frame != null).ToArray();
            if (frames.Length == 0) continue;
            float length = clip.length > 0f ? clip.length : frames.Length / 8f;
            return (frames, frames.Length / length);
        }
        return (null, 8f);
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = System.IO.Path.GetDirectoryName(folder).Replace('\\', '/');
        EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, System.IO.Path.GetFileName(folder));
    }
}
