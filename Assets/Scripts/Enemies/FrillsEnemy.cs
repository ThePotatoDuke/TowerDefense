using UnityEngine;

// Sky-high flyer: follows the path well above it, out of reach of anything but guns that can aim up.
// Its speed changes evenly from a start speed to an end speed along the path, so variants can keep a
// steady pace, slow down or speed up as they near the castle. Every few seconds it shoots at the player
// when they're in range: a marker shows on the ground where the shot will land (EnemyBullet).
public class FrillsEnemy : EnemyBase
{
    // Above this the weapons treat it as flying (only guns that aim up target it)
    private const float FlyingFrom = 1f;

    [SerializeField] private FrillsDataSO data;
    [Tooltip("Child that floats above the path while the root stays on it. Holds the sprite, collider and health bar.")]
    [SerializeField] private Transform body;

    private Vector3 bodyRestPosition;
    private SpriteRenderer sprite;
    private float age;
    private float bobPhase;
    private float height;
    private float shootTimer;
    private float animationTime;

    public override EnemyDataSO Data => data;
    public override bool IsFlying => height > FlyingFrom;
    // High above anything rolling along the path
    public override bool CanDodge => false;

    // Speed for where it is along the path
    private float CurrentSpeed => Mathf.Lerp(data.startSpeed, data.endSpeed, distancePercentage);

    protected override void Awake()
    {
        base.Awake();

        if (body == null && transform.childCount > 0)
            body = transform.GetChild(0);
        if (body != null)
            bodyRestPosition = body.localPosition;
        sprite = body != null ? body.GetComponentInChildren<SpriteRenderer>() : GetComponentInChildren<SpriteRenderer>();

        // A group doesn't bob or shoot in step
        bobPhase = Random.value * Mathf.PI * 2f;
        shootTimer = Random.Range(data.minShootInterval, data.maxShootInterval);
        animationTime = Random.value;
    }

    protected override void Update()
    {
        if (isDead) return;

        base.Update(); // root follows the spline, castle damage
        Move();
        UpdateBody();
        Animate();
        UpdateShooting();
    }

    public override void Move()
    {
        if (hasReachedEnd || splineContainer == null) return;
        distancePercentage = Mathf.Clamp01(distancePercentage + CurrentSpeed * Time.deltaTime / splineLength);
    }

    // Rises to flying height after spawning, then floats there
    private void UpdateBody()
    {
        if (body == null) return;

        float dt = Time.deltaTime;
        age += dt;
        bobPhase += data.bobsPerSecond * Mathf.PI * 2f * dt;

        float rise = Mathf.SmoothStep(0f, 1f, age / Mathf.Max(0.01f, data.takeOffTime));
        height = data.flyHeight * rise;
        body.position = transform.TransformPoint(bodyRestPosition) + Vector3.up * (height + Mathf.Sin(bobPhase) * data.bobHeight * rise);
    }

    private void Animate()
    {
        if (sprite == null || data.idleFrames == null || data.idleFrames.Length == 0) return;
        animationTime += Time.deltaTime;
        sprite.sprite = data.idleFrames[(int)(animationTime * data.idleFramesPerSecond) % data.idleFrames.Length];
    }

    // Shoots at where the player stands, now and then, while they're in range
    private void UpdateShooting()
    {
        shootTimer -= Time.deltaTime;
        if (shootTimer > 0f) return;

        Player player = Player.Instance;
        if (player == null || player.IsDead() || GameStateManager.CurrentState != GameState.Playing || sprite == null)
        {
            shootTimer = 0.5f;
            return;
        }

        // On the ground under the player (the path is at ground level, as is this enemy's root)
        Vector3 target = player.transform.position;
        target.y = transform.position.y;
        Vector3 across = target - transform.position;
        across.y = 0f;
        if (across.magnitude > data.shootRange)
        {
            shootTimer = 0.5f; // look again shortly
            return;
        }

        shootTimer = Random.Range(data.minShootInterval, data.maxShootInterval);
        EnemyBullet.Fire(sprite.bounds.center, target, data);
    }
}
