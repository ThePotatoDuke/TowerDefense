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

        playerHand.DOLocalRotate(new Vector3(0, endAngle, 0), swingDuration)
            .SetEase(Ease.InOutSine)
            .OnComplete(() =>
            {

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

        target.TakeDamage(MeleeData.damage);

        // Track the enemy to avoid multiple hits
        if (targetMB != null)
            alreadyHitEnemies.Add(targetMB.gameObject);
    }





}
