using System;
using System.Collections.Generic;
using UnityEngine;

// Every sound effect in the game, each picked by name (Sound) where it happens, with its clip and how it's
// played. Sounds from things in the world are heard by the player: louder close by, fading out with
// distance, and from the side they're on. Lives in Resources; Tools > Set Up Sounds fills it in from
// Assets/Audio/sfx.
// Numbered, as the library stores them by number: keep the numbers when adding or removing one.
public enum Sound
{
    ButtonClick = 0,
    MenuTransition = 1,
    Pause = 2,
    GameOver = 3,
    WaveClear = 4,
    ShopWaveClear = 5,
    Collect = 6,
    EnemyDie = 7,
    PlayerHit = 8,
    CastleCollapse = 10,
    CrimsonPlant = 11,
    CrimsonShot = 12,
    SporeSmallExplosion = 13,
    SporeBigExplosion = 14,
    BombThrow = 15,
    BombBuildup = 16,
    MetalSpark = 17,
    TibbitDrop = 19,
    PlayerDeath = 20,
    PlayerFall = 21,
    SwordSwing = 22,
    EnemyHit = 23,
    LaserShot = 24,
    LaserHitGround = 25,
    SpecialWaveBanner = 26,
    SpikeDrop = 27,
    GrassCut = 28,
    NextWave = 29,
    EnemyJump = 30,
    TeleportCharge = 31, // loops while the totem's warp is channelled
    Teleport = 32,
    MenuSignChange = 33
}

[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [Serializable]
    public class Entry
    {
        public Sound sound;
        public AudioClip clip;
        [Tooltip("Other clips for the same sound: each play picks one of these or the clip at random.")]
        public List<AudioClip> alternatives = new();
        [Tooltip("1 = as loud as the file. Above 1 boosts it, and too much can distort it. A looping sound " +
                 "(the castle's collapse) stops at 1.")]
        [Range(0f, 3f)] public float volume = 1f;
        [Tooltip("Each play is pitched up or down by up to this much at random, so repeats don't sound stamped.")]
        [Range(0f, 0.5f)] public float pitchVariation = 0.05f;
        [Tooltip("Seconds before it can play again; plays sooner than that are skipped, so a crowd dying at " +
                 "once isn't deafening.")]
        public float minGap = 0.05f;

        public AudioClip Pick()
        {
            int pick = UnityEngine.Random.Range(0, alternatives.Count + 1);
            return pick < alternatives.Count && alternatives[pick] != null ? alternatives[pick] : clip;
        }
    }

    private static SoundLibrary instance;
    public static SoundLibrary Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<SoundLibrary>("SoundLibrary");
            return instance;
        }
    }

    [Range(0f, 1f)] public float masterVolume = 0.8f;

    [Header("Hearing (sounds from the world)")]
    [Tooltip("Within this distance of the player (world units) a sound is at full volume.")]
    public float fullVolumeDistance = 4f;
    [Tooltip("At this distance and beyond, it can't be heard.")]
    public float silentDistance = 18f;
    [Tooltip("How far to the left or right speaker a sound off to the side goes (0 = always in the middle).")]
    [Range(0f, 1f)] public float stereoWidth = 0.6f;

    public List<Entry> sounds = new();

    public Entry Get(Sound sound) => sounds.Find(entry => entry.sound == sound);
}
