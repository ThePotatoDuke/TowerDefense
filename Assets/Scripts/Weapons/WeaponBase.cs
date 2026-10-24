using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    public abstract WeaponDataSO Data { get; }

    protected GameObject owner;
    protected Transform playerHand;
    private Player ownerPlayer;

    private float lastAttackTime;

    public void Initialize(GameObject owner, Transform hand)
    {
        this.owner = owner;
        playerHand = hand;
        ownerPlayer = owner.GetComponent<Player>();
    }

    private void Update()
    {
        if (GameStateManager.CurrentState != GameState.Playing) return;
        // No attacks during the death animation, before the game-over state kicks in
        if (ownerPlayer != null && ownerPlayer.IsDead()) return;

        GameObject target = FindClosestEnemy();
        if (target == null)
            target = FindIdleTarget();
        if (target != null)
            TryAttack(target);
    }

    // Something to attack when no enemy is in range (the sword mows grass)
    protected virtual GameObject FindIdleTarget() => null;

    public void TryAttack(GameObject target)
    {
        float cooldown = 1f / Data.attackRate;
        if (Time.time < lastAttackTime + cooldown) return;

        lastAttackTime = Time.time;
        OnAttack(target);
    }

    protected abstract void OnAttack(GameObject target);

    // Skip enemies this weapon can't reach, so it doesn't waste attacks on them
    protected virtual bool CanTarget(EnemyBase enemy) => true;

    private GameObject FindClosestEnemy()
    {
        float closestDistance = float.MaxValue;
        GameObject closest = null;

        if (owner == null) return null;

        Collider[] colliders = Physics.OverlapSphere(owner.transform.position, Data.range);
        foreach (var c in colliders)
        {
            var enemy = c.GetComponentInParent<EnemyBase>();
            if (enemy != null && !enemy.IsDead && CanTarget(enemy))
            {
                float dist = Vector3.Distance(owner.transform.position, enemy.transform.position);
                if (dist < closestDistance)
                {
                    closestDistance = dist;
                    closest = enemy.gameObject;
                }
            }
        }

        return closest;
    }

}
