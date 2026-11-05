using UnityEditor;
using UnityEngine;

// Tools > Set Up Sounds: makes Resources/SoundLibrary if there isn't one and gives every sound that has no
// clip yet its file from Assets/Audio/sfx (sounds already set, and their volumes, are kept).
public static class SoundSetup
{
    private const string LibraryPath = "Assets/Resources/SoundLibrary.asset";
    private const string Folder = "Assets/Audio/sfx/";

    private static string FileFor(Sound sound) => sound switch
    {
        Sound.ButtonClick => "ui/button_click.ogg",
        Sound.MenuTransition => "ui/menu_transition.ogg",
        Sound.Pause => "ui/pause.ogg",
        Sound.GameOver => "ui/game_over.ogg",
        Sound.WaveClear => "ui/wave_clear.ogg",
        Sound.ShopWaveClear => "gameplay/shop_wave_clear.ogg",
        Sound.Collect => "gameplay/collect.ogg",
        Sound.EnemyDie => "enemies/enemy_die.ogg",
        Sound.PlayerHit => "gameplay/player_hit.ogg",
        Sound.CastleCollapse => "gameplay/castle_die_loop.ogg",
        Sound.CrimsonPlant => "gameplay/crimson_weed_plant.ogg",
        Sound.CrimsonShot => "gameplay/crimson_shot.ogg",
        Sound.SporeSmallExplosion => "gameplay/spore_small_explosion.ogg",
        Sound.SporeBigExplosion => "gameplay/spore_big_explosion.ogg",
        Sound.BombThrow => "enemies/bomb_throw.ogg",
        Sound.BombBuildup => "enemies/bomb_explosoion_buildup.ogg",
        Sound.MetalSpark => "enemies/metal_spark.ogg",
        Sound.TibbitDrop => "enemies/tibbit_drop.ogg",
        Sound.PlayerDeath => "gameplay/player_death.ogg",
        Sound.PlayerFall => "gameplay/player_fall.ogg",
        Sound.SwordSwing => "gameplay/sword_swing_slow.wav",
        Sound.EnemyHit => "enemies/enemy_hit.wav",
        Sound.LaserShot => "enemies/laser.wav",
        Sound.LaserHitGround => "enemies/laser_hit_ground.wav",
        Sound.SpecialWaveBanner => "ui/special_wave_banner.mp3",
        Sound.SpikeDrop => "enemies/spike_drop.wav",
        Sound.GrassCut => "gameplay/grass_cut_2.wav",
        Sound.NextWave => "ui/next_wave.wav",
        Sound.EnemyJump => "enemies/enemy_jump.wav",
        _ => null
    };

    // A second clip for sounds that pick between them at random
    private static string AlternativeFor(Sound sound) => sound switch
    {
        Sound.SwordSwing => "gameplay/sword_swing_slow_2.wav",
        Sound.LaserShot => "enemies/laser_2.wav",
        Sound.GrassCut => "gameplay/grass_cut_3.wav",
        _ => null
    };

    [MenuItem("Tools/Set Up Sounds")]
    private static void SetUp()
    {
        var library = AssetDatabase.LoadAssetAtPath<SoundLibrary>(LibraryPath);
        if (library == null)
        {
            library = ScriptableObject.CreateInstance<SoundLibrary>();
            AssetDatabase.CreateAsset(library, LibraryPath);
        }

        int filled = 0;
        foreach (Sound sound in System.Enum.GetValues(typeof(Sound)))
        {
            SoundLibrary.Entry entry = library.Get(sound);
            if (entry == null)
            {
                entry = new SoundLibrary.Entry { sound = sound };
                // Quick repeats are part of these
                if (sound is Sound.Collect or Sound.CrimsonShot or Sound.BombBuildup)
                    entry.minGap = 0.03f;
                // These shouldn't overlap themselves
                // A swing mows many tufts at once: one cut sound for the lot
                if (sound is Sound.GrassCut)
                    entry.minGap = 0.1f;
                if (sound is Sound.WaveClear or Sound.ShopWaveClear or Sound.GameOver or Sound.MenuTransition
                    or Sound.CastleCollapse)
                    entry.minGap = 0.3f;
                library.sounds.Add(entry);
            }
            string alternative = AlternativeFor(sound);
            if (alternative != null && entry.alternatives.Count == 0)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + alternative);
                if (clip != null)
                    entry.alternatives.Add(clip);
            }
            if (entry.clip != null) continue;

            string file = FileFor(sound);
            entry.clip = file != null ? AssetDatabase.LoadAssetAtPath<AudioClip>(Folder + file) : null;
            if (entry.clip != null)
                filled++;
            else
                Debug.LogWarning($"Sounds: no clip found for {sound} ({Folder}{file}).");
        }

        EditorUtility.SetDirty(library);
        AssetDatabase.SaveAssets();
        Selection.activeObject = library;
        Debug.Log($"Sounds: {filled} clips set in {LibraryPath}.");
    }
}
