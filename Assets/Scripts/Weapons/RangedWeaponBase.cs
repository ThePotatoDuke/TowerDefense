using UnityEngine;

public abstract class RangedWeaponBase : WeaponBase
{
    [SerializeField] private RangedWeaponDataSO rangedData;
    public override WeaponDataSO Data => rangedData;
    protected RangedWeaponDataSO RangedData => rangedData;

    // Flat-shooting guns fire under flying enemies
    // Not enemies its shots would only bounce off
    protected override bool CanTarget(EnemyBase enemy) => (RangedData.canAimUp || !enemy.IsFlying) && !enemy.ImmuneToRanged;

    // Chance (0 to 1) that a shot homes in on its target, from the gun's homing upgrade
    protected virtual float HomingChance => 0f;

    private static readonly Color HomingTint = new(1f, 0.25f, 0.25f);
    private const float HomingTurnSpeed = 300f; // degrees a second

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
        proj.Initialize(RangedData.projectileData, dir, DamageMultiplier);

        // Now and then a red shot that homes in (and on to the next enemy in range once it's gone)
        if (Random.value < HomingChance)
            proj.MakeHoming(target.GetComponentInParent<EnemyBase>(), CanTarget, !RangedData.canAimUp,
                            HomingTurnSpeed, Range * 1.5f, HomingTint);
    }



}
