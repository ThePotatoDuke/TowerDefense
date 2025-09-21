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

        if (weaponCollider != null) weaponCollider.enabled = true;
        alreadyHitEnemies.Clear();

        Vector3 dir = targetPosition - playerHand.position;
        dir.y = 0;
        if (dir == Vector3.zero) return;

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
        if (alreadyHitEnemies.Contains(other.gameObject)) return;

        if (other.TryGetComponent<IHasHealth>(out var target))
        {
            target.TakeDamage(MeleeData.damage);
            alreadyHitEnemies.Add(other.gameObject);
        }
    }

}
