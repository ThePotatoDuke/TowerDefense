using UnityEngine;

public abstract class EnemyDataSO : ScriptableObject
{
    [Header("General Settings")]
    public string enemyName;

    public float maxHealth = 10f;
    public float contactDamage = 1f;

    [Header("Spawning")]
    [Tooltip("Instead of coming in at the start of the path, it drops from the sky onto a marked spot along it.")]
    public bool dropsFromSky;
    [Tooltip("Waves can give it a shield against ranged attacks at random (Wave Settings > Shields). Off for " +
             "ones a shield would make unfair, like the Hyper Slime; flying enemies never get one.")]
    public bool canBeShielded = true;

    [Header("On Death")]
    [Tooltip("Optional: when killed it splits into this many of these (a Splitter Slime into Mini Slimes).")]
    public EnemyBase splitInto;
    public int splitCount = 2;
    [Tooltip("How far apart along the path the pieces land (world units).")]
    public float splitSpread = 0.8f;
}
