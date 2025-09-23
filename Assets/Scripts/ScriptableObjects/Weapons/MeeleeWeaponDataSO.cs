using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/MeleeWeaponData")]
public class MeleeWeaponDataSO : WeaponDataSO
{
    [Header("Melee Specific")]
    public float swingDuration = 0.1f;
    public float attackRange = 1f;
    public float damage = 5f;
}
