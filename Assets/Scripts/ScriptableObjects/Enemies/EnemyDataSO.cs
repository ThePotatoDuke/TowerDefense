using UnityEngine;

public enum EnemyMovementType { Walking, Bouncing }

public abstract class EnemyDataSO : ScriptableObject
{
    [Header("General Settings")]
    public string enemyName;
    public Color color = Color.white;
    public float maxHealth = 10f;
    public float contactDamage = 1f;
}
