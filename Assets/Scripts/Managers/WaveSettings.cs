using System.Collections.Generic;
using UnityEngine;

// The run's waves and how they get harder, for EndlessWaveManager (which uses these instead of its own
// list when this asset has waves). The hand-made waves bring in one new kind of enemy at a time; once
// they've all been played the run loops back to a later wave, each loop with more enemies arriving
// faster, and enemies get a little tougher every wave throughout. Each wave also has a timer: clearing it
// before the time runs out pays a harvest bonus, a share of the grass picked up during the wave that's
// bigger the more time is left. Every few waves a boss wave comes in between (one of the boss waves at
// random, each seen before any comes round again): a gimmick with a banner of its own, its own music, a
// bigger harvest bonus and a shop after it. Lives in Resources.
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

    [Header("Harvest Bonus")]
    [Tooltip("Seconds every wave gives to earn the harvest bonus in, before the time per enemy is added (a " +
             "wave can set its own Time Limit instead). The Lasting Harvest upgrade stretches it.")]
    public float timerBase = 30f;
    [Tooltip("Extra seconds for each enemy the wave spawns.")]
    public float timerPerEnemy = 2f;
    [Tooltip("The bonus, as a percentage of the grass picked up during the wave, for clearing it straight " +
             "away; it shrinks evenly to nothing as the timer runs down. The Bumper Crop upgrade adds to it.")]
    public float maxBonusPercent = 50f;

    [Header("Boss Waves")]
    [Tooltip("Every this many waves is a boss wave (10 = waves 10, 20, 30...), in between the waves above, " +
             "which carry on after it. 0 = none.")]
    public int bossWaveEvery = 10;
    [Tooltip("A boss wave's harvest bonus is this many times the usual.")]
    public float bossBonusMultiplier = 2f;
    [Tooltip("Seconds the banner shows before a boss wave's enemies start coming.")]
    public float bossBannerTime = 2.5f;
    [Tooltip("Picked at random; each one is played before any comes round again. Tools > Set Up Boss Waves " +
             "fills in a starting set.")]
    public List<BossWave> bossWaves = new();

    public bool IsBossWave(int wave) => bossWaveEvery > 0 && bossWaves.Count > 0 && wave % bossWaveEvery == 0;

    public float HealthMultiplier(int wave) => 1f + healthGrowthPerWave * Mathf.Max(0, wave - 1);

    // Seconds to earn the harvest bonus in for a wave of this many enemies, stretched by the Lasting Harvest
    // upgrade (so the bonus drains slower)
    public float TimeLimit(Wave wave, int enemies)
    {
        float time = wave.timeLimit > 0f ? wave.timeLimit : timerBase + timerPerEnemy * enemies;
        return time * Upgrades.Multiplier(UpgradeType.LastingHarvest);
    }

    // The bonus percentage at the start of a wave, with the Bumper Crop upgrade
    public float StartBonusPercent => maxBonusPercent + Upgrades.Bonus(UpgradeType.BumperCrop);

    // The bonus percentage with this share of the timer left (1 = all of it)
    public float BonusPercent(float timeLeftShare) => StartBonusPercent * Mathf.Clamp01(timeLeftShare);
}

// A boss wave: a wave with a gimmick. Its enemies come like any wave's; on top of that it can darken the
// scene (Bat Cave) and drop spike balls around the player all through it (Spike Rain). Each time it comes
// round again it's bigger.
[System.Serializable]
public class BossWave : Wave
{
    [Tooltip("Big in the middle of the screen as it starts, e.g. \"Konga Express!\".")]
    public string bannerTitle = "Boss Wave!";
    [Tooltip("Smaller, under the title.")]
    public string bannerSubtitle = "BOSS WAVE";

    [Tooltip("The scene darkens through the wave.")]
    public bool dark;

    [Tooltip("Spike balls fall around the player all through the wave and burst into spikes where they land. " +
             "Its spikes, their damage and the ball's look come from this spike ball (one with a spike burst).")]
    public SpikeBallEnemy spikeRain;
    [Tooltip("Seconds between falling spike balls (a little random either way).")]
    public float spikeRainInterval = 1.6f;
    [Tooltip("How far from the player they land, at most (world units).")]
    public float spikeRainRadius = 4f;

    [Tooltip("Each time it comes round again, its Base Counts grow by this share (0.3 = 30% more) and the " +
             "spike rain comes this much faster.")]
    public float growthPerRepeat = 0.3f;
}
