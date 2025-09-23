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
            targetPos = childCollider.bounds.center;
        else
            targetPos = target.transform.position; // fallback

        // Direction vector from hand to target
        Vector3 dir = targetPos - playerHand.position;

        // Restrict vertical aiming if canAimUp is false
        if (!RangedData.canAimUp)
        {
            dir.y = 0f; // ignore vertical difference
            if (dir == Vector3.zero)
                dir = playerHand.forward; // fallback
            dir.Normalize();
        }
        else
        {
            if (dir == Vector3.zero)
                dir = Vector3.forward;
            dir.Normalize();
        }

        // Spawn projectile
        Projectile proj = Instantiate(RangedData.projectilePrefab, playerHand.position, Quaternion.identity);
        proj.Initialize(RangedData.projectileData, dir);
    }



}
