using UnityEngine;

public abstract class RangedWeaponBase : WeaponBase
{
    [SerializeField] private RangedWeaponDataSO rangedData;
    public override WeaponDataSO Data => rangedData;
    protected RangedWeaponDataSO RangedData => rangedData;

    protected override void OnAttack(GameObject target)
    {
        // Try to find a collider in the children
        Collider childCollider = target.GetComponentInChildren<Collider>();
        Vector3 targetPos;

        if (childCollider != null)
        {
            targetPos = childCollider.bounds.center;
        }
        else
        {
            targetPos = target.transform.position; // fallback to parent
        }

        // Direction vector
        Vector3 dir = (targetPos - playerHand.position).normalized;

        // Clamp Y if projectile cannot aim down
        if (!RangedData.canAimUp)
        {
            dir.y = Mathf.Max(dir.y, 0f);
            dir.Normalize();
        }

        if (dir == Vector3.zero) dir = Vector3.forward;

        // Spawn projectile
        Projectile proj = Instantiate(RangedData.projectilePrefab, playerHand.position, Quaternion.identity);
        proj.Initialize(RangedData.projectileData, dir);
    }


}
