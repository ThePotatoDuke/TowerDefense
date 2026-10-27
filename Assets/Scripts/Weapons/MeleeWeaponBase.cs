using UnityEngine;
using System.Collections.Generic;
using DG.Tweening;

public abstract class MeleeWeaponBase : WeaponBase
{

    [SerializeField] private MeleeWeaponDataSO data;
    public override WeaponDataSO Data => data;   // safely expose as base
    protected MeleeWeaponDataSO MeleeData => data; // strongly typed access
    [SerializeField] protected Collider weaponCollider;

    protected HashSet<GameObject> alreadyHitEnemies = new HashSet<GameObject>();
    private SwordTrail swordTrail;
    private Collider blade;
    // The blade's holder (the weapon's first child), grown by size upgrades
    private Transform holder;
    private Vector3 holderScale;
    private Vector3 holderOffset;

    // Shop upgrade making the blade bigger (and reach further): 1 = none bought
    protected virtual float SizeMultiplier => 1f;
    protected override float RangeMultiplier => SizeMultiplier;

    protected virtual void Awake()
    {
        // weaponCollider may be unassigned on the prefab; the trail only needs the blade shape
        blade = weaponCollider != null ? weaponCollider : GetComponent<Collider>();
        swordTrail = gameObject.GetOrAddComponent<SwordTrail>();
        swordTrail.Setup(blade as BoxCollider);

        if (transform.childCount > 0)
        {
            holder = transform.GetChild(0);
            holderScale = holder.localScale;
            holderOffset = holder.localPosition;
        }
        ApplySize();
        Upgrades.OnUpgraded += HandleUpgraded;
    }

    protected virtual void OnDestroy()
    {
        Upgrades.OnUpgraded -= HandleUpgraded;
    }

    private void HandleUpgraded(UpgradeType type) => ApplySize();

    // Scales the blade from the hand outwards, so it gets both longer and further from the hand
    private void ApplySize()
    {
        if (holder == null) return;
        holder.localScale = holderScale * SizeMultiplier;
        holder.localPosition = holderOffset * SizeMultiplier;
    }

    // Put away mid-swing (switched to another weapon): stop the swing so the hand isn't left turning
    // and the blade doesn't stay live
    protected virtual void OnDisable()
    {
        if (playerHand != null) playerHand.DOKill();
        if (swordTrail != null) swordTrail.End();
        if (weaponCollider != null) weaponCollider.enabled = false;
    }

    // Swings stay at hand height, below flying enemies
    protected override bool CanTarget(EnemyBase enemy) => !enemy.IsFlying;

    // With no enemy around, swing at nearby grass instead
    protected override GameObject FindIdleTarget()
    {
        if (GroundDecorator.Instance == null || owner == null) return null;
        return GroundDecorator.Instance.FindStandingTuft(owner.transform.position, Range);
    }

    protected void SwingWeapon(Vector3 targetPosition, float swingDuration, float swingArc)
    {

        alreadyHitEnemies.Clear();

        Vector3 dir = targetPosition - playerHand.position;
        dir.y = 0;
        if (dir == Vector3.zero) return;

        if (weaponCollider != null) weaponCollider.enabled = true;

        float angle = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        float startAngle = angle - swingArc;
        float endAngle = angle + swingArc;

        Vector3 euler = playerHand.localEulerAngles;
        euler.y = startAngle;
        playerHand.localEulerAngles = euler;
        swordTrail.Begin();

        playerHand.DOLocalRotate(new Vector3(0, endAngle, 0), swingDuration)
            .SetEase(Ease.InOutSine)
            .OnUpdate(() =>
            {
                // Mow any grass the blade passes through
                if (blade != null && GroundDecorator.Instance != null)
                    GroundDecorator.Instance.CutGrass(blade);
            })
            .OnComplete(() =>
            {
                // A tuft swung at always falls, even if it sat just past the blade's tip
                if (GroundDecorator.Instance != null)
                    GroundDecorator.Instance.CutTuftAt(targetPosition, blade != null ? blade.transform.position : targetPosition);

                if (swordTrail != null) swordTrail.End();
                if (weaponCollider != null) weaponCollider.enabled = false;
            });
    }

    protected virtual void OnTriggerEnter(Collider other)
    {
        // Look for IHasHealth on the collider or any of its parents
        IHasHealth target = other.GetComponentInParent<IHasHealth>();
        if (target == null) return;

        // Ignore the owner (player)
        MonoBehaviour targetMB = target as MonoBehaviour;
        if (targetMB != null && targetMB.gameObject == owner) return;

        // Avoid hitting the same enemy multiple times per swing (keyed by the health owner, not the child collider)
        if (targetMB != null && alreadyHitEnemies.Contains(targetMB.gameObject)) return;

        target.TakeDamage(MeleeData.damage * DamageMultiplier);

        // Track the enemy to avoid multiple hits
        if (targetMB != null)
            alreadyHitEnemies.Add(targetMB.gameObject);
    }





}
