using System.Collections.Generic;
using UnityEngine;

// The run's waves and how they get harder, for EndlessWaveManager (which uses these instead of its own
// list when this asset has waves). The hand-made waves bring in one new kind of enemy at a time; once
// they've all been played the run loops back to a later wave, each loop with more enemies arriving
// faster, and enemies get a little tougher every wave throughout. Each wave also has a timer to clear it
// in; when it runs out the wave goes into overtime and the Time Up penalty kicks in. Lives in Resources.
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

    [Header("Wave Timer")]
    [Tooltip("Seconds every wave gets to be cleared in, before the time per enemy is added (a wave can set " +
             "its own Time Limit instead). The Extra Time upgrade adds to it.")]
    public float timerBase = 30f;
    [Tooltip("Extra seconds for each enemy the wave spawns.")]
    public float timerPerEnemy = 2f;
    [Tooltip("What happens when a wave's timer runs out before it's cleared.")]
    public TimeUpPenalty timeUpPenalty = TimeUpPenalty.DrainCastle;
    [Tooltip("Drain Castle: the castle loses this much health every second of overtime, until the wave is cleared.")]
    public float overtimeCastleDamage = 2f;

    public float HealthMultiplier(int wave) => 1f + healthGrowthPerWave * Mathf.Max(0, wave - 1);

    // Seconds to clear a wave of this many enemies in, with the Extra Time upgrade
    public float TimeLimit(Wave wave, int enemies)
    {
        float time = wave.timeLimit > 0f ? wave.timeLimit : timerBase + timerPerEnemy * enemies;
        return time + Upgrades.Bonus(UpgradeType.ExtendTime);
    }
}

public enum TimeUpPenalty
{
    [Tooltip("The castle loses health every second until the wave is cleared.")]
    DrainCastle,
    [Tooltip("The run ends straight away (the castle falls).")]
    GameOver,
    [Tooltip("Nothing: the timer only shows how long it's taking.")]
    None
}
