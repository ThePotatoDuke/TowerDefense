using System.Collections.Generic;
using UnityEngine;

// The run's waves and how they get harder, for EndlessWaveManager (which uses these instead of its own
// list when this asset has waves). The hand-made waves bring in one new kind of enemy at a time; once
// they've all been played the run loops back to a later wave, each loop with more enemies arriving
// faster, and enemies get a little tougher every wave throughout. Lives in Resources.
[CreateAssetMenu(fileName = "WaveSettings", menuName = "Waves/Wave Settings")]
public class WaveSettings : ScriptableObject
{
    private static WaveSettings instance;
    public static WaveSettings Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<WaveSettings>("WaveSettings");
            return instance;
        }
    }

    [Header("Pacing")]
    [Tooltip("Seconds before the first wave, to look around.")]
    public float firstWaveDelay = 3f;
    [Tooltip("Seconds of calm after a wave is cleared (and after the shop) before the next one.")]
    public float breakBetweenWaves = 3f;

    [Header("Waves")]
    public List<Wave> waves = new();

    [Header("After The Last Wave")]
    [Tooltip("The run carries on from this wave (1 = the first) once the list has been played through.")]
    public int loopFromWave = 9;
    [Tooltip("Extra random enemies (picked by the wave's spawn weights) added to each wave, per loop.")]
    public int extraEnemiesPerLoop = 3;
    [Tooltip("Gaps between spawns are multiplied by this each loop, so enemies come faster.")]
    [Range(0.5f, 1f)] public float spawnGapPerLoop = 0.85f;
    [Tooltip("Shortest gap between spawns, however many loops in.")]
    public float minSpawnGap = 0.2f;

    [Header("Toughness")]
    [Tooltip("Enemy health grows by this fraction every wave after the first (0.04 = +4% a wave, " +
             "so wave 26 enemies have double health).")]
    public float healthGrowthPerWave = 0.04f;

    public float HealthMultiplier(int wave) => 1f + healthGrowthPerWave * Mathf.Max(0, wave - 1);
}
