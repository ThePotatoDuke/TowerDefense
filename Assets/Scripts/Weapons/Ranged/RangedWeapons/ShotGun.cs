using UnityEngine;

// The Hoop Cannon: slow, heavy rings that punch through an enemy
public class ShotGun : RangedWeaponBase
{
    protected override float DamageMultiplier => Upgrades.Multiplier(UpgradeType.HoopCannonDamage);
    protected override float AttackRateMultiplier => Upgrades.Multiplier(UpgradeType.HoopCannonFireRate);
    protected override float RangeMultiplier => Upgrades.Multiplier(UpgradeType.HoopCannonRange);
    protected override float HomingChance => Upgrades.Bonus(UpgradeType.HoopCannonHoming) / 100f;
}
