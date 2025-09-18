using UnityEngine;

public abstract class RangedWeaponBase : WeaponBase
{
    [SerializeField] private RangedWeaponDataSO rangedData;
    public override WeaponDataSO Data => rangedData;
    protected RangedWeaponDataSO RangedData => rangedData;



    protected override void OnAttack(GameObject target)
    {
        Vector3 dir = (target.transform.position - playerHand.position).normalized;

        if (!RangedData.canAimUp)
            dir.y = 0; // keep horizontal

        if (dir == Vector3.zero) dir = Vector3.forward;

        // Spawn projectile
        Projectile proj = Instantiate(RangedData.projectilePrefab, playerHand.position, Quaternion.identity);

        // Rotate the visual child around local Y
        proj.SetDirection(dir);

        proj.Init(RangedData.projectileData, dir);
    }
}
