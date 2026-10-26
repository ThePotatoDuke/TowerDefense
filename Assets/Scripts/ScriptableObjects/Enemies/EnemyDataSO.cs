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
}
