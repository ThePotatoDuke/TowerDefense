using UnityEngine;

public class DefaultSword : MeleeWeaponBase
{
    protected override void OnAttack(GameObject target)
    {
        alreadyHitEnemies.Clear();
        SwingWeapon(target.transform.position, MeleeData.swingDuration, 60f); // sword arc
    }
}
