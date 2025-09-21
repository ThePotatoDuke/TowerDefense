using UnityEngine;

[CreateAssetMenu(menuName = "Weapons/RangedWeaponData")]
public class RangedWeaponDataSO : WeaponDataSO
{
    [Header("Ranged Specific")]
    public Projectile projectilePrefab;
    public ProjectileDataSO projectileData;
    public bool canAimUp = false;
}
