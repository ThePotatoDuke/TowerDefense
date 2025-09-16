using UnityEngine;

public abstract class WeaponDataSO : ScriptableObject
{
    [Header("General Stats")]
    public float damage = 10f;
    public float attackRate = 1f;         // attacks per second
    public float knockbackDistance = 1f;
    public float knockbackDuration = 0.2f;
}
