using System;
using UnityEngine;

public abstract class WeaponDataSO : ScriptableObject
{
    [Header("General Stats")]
    public float attackRate = 1f;         // attacks per second
    public float knockbackDistance = 1f;
    public float knockbackDuration = 0.2f;
    public float range = 10f;
}
