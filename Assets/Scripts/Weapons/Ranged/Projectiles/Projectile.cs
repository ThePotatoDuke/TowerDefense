using System.Collections.Generic;
using UnityEngine;

public class Projectile : MonoBehaviour
{
    private ProjectileDataSO data;
    private Vector3 direction;
    private int pierceCount;
    private ProjectileFX fx;
    private bool deflected; // bounced off an immune enemy: harmless from then on
    private float damageMultiplier = 1f; // from the shop's damage upgrades for the gun that fired it
    private System.Action<EnemyBase> onHit;      // told about each enemy it's about to hurt (Crimson Weed growing)

    // Homing (the shop's homing upgrades): turns toward its target, or the closest enemy it could hit once
    // that's gone or been pierced
    private float homingTurnSpeed; // degrees a second; 0 = flies straight
    private EnemyBase homingTarget;
    private System.Func<EnemyBase, bool> canHome;
    private bool homingFlat;
    private float homingReach;
    private readonly HashSet<EnemyBase> alreadyHit = new();
    private float visualYaw;

    private void Awake()
    {
        // Automatically find the child named "BulletVisual"
        bulletVisual = transform.Find("BulletVisual");
        if (bulletVisual == null)
            Debug.LogWarning("BulletVisual child not found!");

        fx = gameObject.GetOrAddComponent<ProjectileFX>();
    }
    public void Initialize(ProjectileDataSO data, Vector3 direction, float damageMultiplier = 1f,
                           System.Action<EnemyBase> onHit = null)
    {
        this.data = data;
        this.damageMultiplier = damageMultiplier;
        this.onHit = onHit;
        this.direction = direction.normalized;
        pierceCount = 0;
        SetDirection();
        Destroy(gameObject, data.lifetime);

    }

    [SerializeField] Transform bulletVisual;

    public void SetDirection()
    {
        // root points along travel direction
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        // optional: add an extra Y spin to the child (e.g., aim sword/gun)
        if (bulletVisual != null)
        {
            float yAngle = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            bulletVisual.localEulerAngles += new Vector3(0f, yAngle, 0f);
            visualYaw = yAngle;
        }
    }

    // Makes it a homing shot: it steers toward the target (then the closest enemy canTarget allows, within
    // reach) at turnSpeed degrees a second, staying level if flat, and is tinted (sprite and trail)
    public void MakeHoming(EnemyBase target, System.Func<EnemyBase, bool> canTarget, bool flat, float turnSpeed,
                           float reach, Color tint)
    {
        homingTarget = target;
        canHome = canTarget;
        homingFlat = flat;
        homingTurnSpeed = turnSpeed;
        homingReach = reach;

        foreach (SpriteRenderer sprite in GetComponentsInChildren<SpriteRenderer>(true))
            sprite.color = tint;
        foreach (TrailRenderer trail in GetComponentsInChildren<TrailRenderer>(true))
        {
            trail.startColor = new Color(tint.r, tint.g, tint.b, trail.startColor.a);
            trail.endColor = new Color(tint.r, tint.g, tint.b, trail.endColor.a);
        }
    }

    private void Steer()
    {
        if (!IsHomeable(homingTarget))
            homingTarget = ClosestTarget();
        if (homingTarget == null) return;

        Vector3 toTarget = AimPoint(homingTarget) - transform.position;
        if (homingFlat)
            toTarget.y = 0f;
        if (toTarget.sqrMagnitude < 0.0001f) return;

        direction = Vector3.RotateTowards(direction, toTarget.normalized,
                                          homingTurnSpeed * Mathf.Deg2Rad * Time.deltaTime, 0f).normalized;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        // The visual's extra spin follows the change of heading, as SetDirection set it up
        if (bulletVisual != null)
        {
            float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
            bulletVisual.localEulerAngles += new Vector3(0f, Mathf.DeltaAngle(visualYaw, yaw), 0f);
            visualYaw = yaw;
        }
    }

    private bool IsHomeable(EnemyBase enemy) =>
        enemy != null && !enemy.IsDead && enemy.gameObject.activeInHierarchy && !alreadyHit.Contains(enemy)
        && (canHome == null || canHome(enemy));

    private EnemyBase ClosestTarget()
    {
        if (EnemyManager.Instance == null) return null;

        EnemyBase closest = null;
        float closestDistance = homingReach;
        foreach (IEnemy candidate in EnemyManager.Instance.ActiveEnemies)
        {
            if (candidate is not EnemyBase enemy || !IsHomeable(enemy)) continue;
            float distance = Vector3.Distance(transform.position, enemy.transform.position);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closest = enemy;
            }
        }
        return closest;
    }

    private static Vector3 AimPoint(EnemyBase enemy)
    {
        Collider body = enemy.GetComponentInChildren<Collider>();
        return body != null ? body.bounds.center : enemy.transform.position;
    }




    private void Update()
    {
        if (homingTurnSpeed > 0f && !deflected)
            Steer();
        transform.position += direction * data.speed * Time.deltaTime;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (deflected) return;

        IEnemy enemy = other.GetComponentInParent<IEnemy>();
        if (enemy is EnemyBase immune && immune.ImmuneToRanged && !immune.IsDead)
        {
            Deflect(immune);
            return;
        }

        if (enemy != null)
        {
            IHasHealth health = enemy as IHasHealth;
            if (health != null)
            {
                // Before the damage, so a kill by this very shot is seen
                if (onHit != null && enemy is EnemyBase hurt && !hurt.IsDead)
                    onHit(hurt);
                health.TakeDamage(data.damage * damageMultiplier);

                if (!data.pierces || pierceCount >= data.maxPierceCount)
                {
                    fx.ReleaseTrail();
                    Destroy(gameObject);
                }
                else
                {
                    pierceCount++;
                    if (enemy is EnemyBase pierced)
                        alreadyHit.Add(pierced); // a homing shot looks for the next one
                }
            }
        }
    }

    // Bounces back the way it came, turned by a random angle and flying up, with a spark where it hit
    private void Deflect(EnemyBase enemy)
    {
        deflected = true;
        FXSettings settings = FXSettings.Instance;
        float spread = settings != null ? settings.deflectSpread : 70f;
        Vector2 climb = settings != null ? settings.deflectClimb : new Vector2(0.6f, 1.4f);

        Vector3 back = new Vector3(-direction.x, 0f, -direction.z);
        if (back.sqrMagnitude < 0.0001f)
            back = Vector3.back;
        back = Quaternion.AngleAxis(Random.Range(-spread, spread), Vector3.up) * back.normalized;
        direction = (back + Vector3.up * Random.Range(climb.x, climb.y)).normalized;
        transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        SparkBurst.Play(transform.position);
        enemy.Flash();
        enemy.NotifyDeflected();
    }

}
