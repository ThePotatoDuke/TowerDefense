using UnityEngine;

public class DefaultSword : MeleeWeaponBase
{
    protected override float DamageMultiplier => Upgrades.Multiplier(UpgradeType.SwordDamage);
    protected override float AttackRateMultiplier => Upgrades.Multiplier(UpgradeType.SwordAttackRate);
    protected override float SizeMultiplier => Upgrades.Multiplier(UpgradeType.SwordSize);

    protected override void OnAttack(GameObject target)
    {
        alreadyHitEnemies.Clear();
        SwingWeapon(target.transform.position, MeleeData.swingDuration, 60f); // sword arc
    }
}
