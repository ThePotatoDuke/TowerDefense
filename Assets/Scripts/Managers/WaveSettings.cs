using System.Collections.Generic;
using UnityEngine;

// The run's waves and how they get harder, for EndlessWaveManager (which uses these instead of its own
// list when this asset has waves). The hand-made waves bring in one new kind of enemy at a time; once
// they've all been played the run loops back to a later wave, each loop with more enemies arriving
// faster, and enemies get a little tougher every wave throughout. Each wave also has a timer: clearing it
// before the time runs out pays a harvest bonus, a share of the grass picked up during the wave that's
// bigger the more time is left. Every few waves a boss wave comes in between (one of the boss waves at
// random, each seen before any comes round again): a gimmick with a banner of its own, its own music, a
// bigger harvest bonus and a shop after it. Waves can have alternatives, one picked at random each time
// the wave comes up, so runs don't all play the same; and from a few waves in, enemies can come with a
// shield against ranged attacks at random, more often the further the run goes. Lives in Resources.
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

    [Tooltip("Other versions of the waves above. Whenever a wave comes up, it or one of its alternatives " +
             "is played, picked at random, so a run doesn't play out the same every time.")]
    public List<WaveAlternative> waveAlternatives = new();

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

    [Header("Shields")]
    [Tooltip("From this wave on, enemies can come with a shield against ranged attacks (the same as a wave " +
             "entry's Immune To Ranged) at random. Never flying ones, or ones whose data says not.")]
    public int shieldsFromWave = 6;
    [Tooltip("Chance of a shield on that first wave.")]
    [Range(0f, 1f)] public float shieldChanceStart = 0.04f;
    [Tooltip("Added to the chance every wave after it.")]
    [Range(0f, 0.1f)] public float shieldChancePerWave = 0.005f;
    [Tooltip("The chance never goes above this.")]
    [Range(0f, 1f)] public float maxShieldChance = 0.15f;
    [Tooltip("Grass blades an enemy with a random shield drops when killed.")]
    public int shieldedGrassDrop = 3;

    [Header("Grass")]
    [Tooltip("Share of the field grown when the game starts; the rest grows in wave by wave.")]
    [Range(0f, 1f)] public float startingGrass = 0.4f;
    [Tooltip("Share of the field that grows back at the start of each wave (as much as has been mown); " +
             "nothing grows during a wave. Abundance raises it.")]
    [Range(0f, 1f)] public float regrowthPerWave = 0.15f;
    [Tooltip("The regrowth rises by this fraction every wave after the first (0.04 = +4% a wave), so later " +
             "waves, with pricier upgrades, pay more.")]
    public float regrowthGrowthPerWave = 0.04f;
    [Tooltip("Grass blades from each mown tuft (Lush Grass adds a chance of more).")]
    public int bladesPerTuft = 1;

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
    [Tooltip("Picked at random; each one is played before any comes round again.")]
    public List<BossWave> bossWaves = new();

    public bool IsBossWave(int wave) => bossWaveEvery > 0 && bossWaves.Count > 0 && wave % bossWaveEvery == 0;

    public float RegrowthShare(int wave) => regrowthPerWave * (1f + regrowthGrowthPerWave * Mathf.Max(0, wave - 1));

    public float HealthMultiplier(int wave) => 1f + healthGrowthPerWave * Mathf.Max(0, wave - 1);

    public float ShieldChance(int wave) => wave < shieldsFromWave ? 0f
        : Mathf.Min(maxShieldChance, shieldChanceStart + shieldChancePerWave * (wave - shieldsFromWave));

    // The wave at this place in the list (0 = the first), or one of its alternatives, at random
    public Wave PickWave(List<Wave> list, int index)
    {
        Wave wave = list[index];
        int number = index + 1, options = 1;
        foreach (WaveAlternative alternative in waveAlternatives)
        {
            // Each one that fits has the same chance as the rest
            if (alternative.replaces != number) continue;
            options++;
            if (Random.Range(0, options) == 0)
                wave = alternative;
        }
        return wave;
    }

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

// Another version of one of the waves in the list, played instead of it now and then
[System.Serializable]
public class WaveAlternative : Wave
{
    [Tooltip("The wave in the list (1 = the first) this can be played instead of.")]
    public int replaces = 1;
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
