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
        }
    }




    private void Update()
    {
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
