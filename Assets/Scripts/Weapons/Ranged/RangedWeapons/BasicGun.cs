using UnityEngine;

// The Blaster: quick shots that can aim up at flyers
public class BasicGun : RangedWeaponBase
{
    protected override float DamageMultiplier => Upgrades.Multiplier(UpgradeType.BlasterDamage);
    protected override float AttackRateMultiplier => Upgrades.Multiplier(UpgradeType.BlasterFireRate);
    protected override float RangeMultiplier => Upgrades.Multiplier(UpgradeType.BlasterRange);
    protected override float HomingChance => Upgrades.Bonus(UpgradeType.BlasterHoming) / 100f;
}
