using UnityEngine;

public abstract class WeaponBase : MonoBehaviour
{
    public abstract WeaponDataSO Data { get; }

    protected GameObject owner;
    protected Transform playerHand;

    private float lastAttackTime;

    public void Initialize(GameObject owner, Transform hand)
    {
        this.owner = owner;
        playerHand = hand;
    }

    private void Update()
    {
        GameObject target = FindClosestEnemy();
        if (target != null)
            TryAttack(target);
    }

    public void TryAttack(GameObject target)
    {
        float cooldown = 1f / Data.attackRate;
        if (Time.time < lastAttackTime + cooldown) return;

        lastAttackTime = Time.time;
        OnAttack(target);
    }

    protected abstract void OnAttack(GameObject target);

    private GameObject FindClosestEnemy()
    {
        float closestDistance = float.MaxValue;
        GameObject closest = null;

        if (owner == null) return null;

        Collider[] colliders = Physics.OverlapSphere(owner.transform.position, Data.range);
        foreach (var c in colliders)
        {
            var enemy = c.GetComponentInParent<EnemyBase>();
            if (enemy != null)
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
