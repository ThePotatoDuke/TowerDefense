using UnityEngine;

// Flying enemy: follows the path just above the ground. Each variant can weave from side to side
// across the path and/or climb and dive; while high, only weapons that can aim up target it.
public class BatEnemy : EnemyBase
{
    [SerializeField] private BatDataSO data;
    [Tooltip("Child that weaves and hovers while the root stays on the path. Holds the sprite, collider and health bar.")]
    [SerializeField] private Transform body;

    private Vector3 bodyRestPosition;
    private float wavePhase;
    private float climbPhase;
    private float bobPhase;
    private float age;
    private float height;

    public override EnemyDataSO Data => data;
    public override bool IsFlying => height > data.aimUpHeight;
    // Already hovering over the path, and UpdateBody positions the body every frame
    public override bool CanDodge => false;

    protected override void Awake()
    {
        base.Awake();

        if (body == null && transform.childCount > 0)
            body = transform.GetChild(0);
        if (body != null)
            bodyRestPosition = body.localPosition;

        // Random phases so a group of bats doesn't weave in lockstep
        wavePhase = Random.value;
        climbPhase = Random.value;
        bobPhase = Random.value * Mathf.PI * 2f;

        gameObject.GetOrAddComponent<BatTrail>().Color = data.trailColor;
    }

    protected override void Update()
    {
        if (isDead) return;

        base.Update(); // root follows the spline, castle damage
        Move();
        UpdateBody();
    }

    public override void Move()
    {
        if (hasReachedEnd || splineContainer == null) return;

        float step = data.moveSpeed * Time.deltaTime / splineLength;
        distancePercentage = Mathf.Clamp01(distancePercentage + step);
    }

    private void UpdateBody()
    {
        if (body == null) return;

        float dt = Time.deltaTime;
        float travelled = data.moveSpeed * dt;
        age += dt;

        // Phases keep running after reaching the end, so bats keep moving over the castle
        wavePhase += travelled / Mathf.Max(0.01f, data.waveLength);
        climbPhase += travelled / Mathf.Max(0.01f, data.climbLength);
        bobPhase += data.flapRate * Mathf.PI * 2f * dt;

        // Ease up from the ground so bats don't pop into the air at the spawn point
        float rise = Mathf.SmoothStep(0f, 1f, age / Mathf.Max(0.01f, data.takeOffTime));

        // 0 at hover height, 1 at the top of the climb
        float climb = 0.5f - 0.5f * Mathf.Cos(climbPhase * Mathf.PI * 2f);
        height = (data.hoverHeight + climb * data.climbHeight) * rise;

        Vector3 side = Vector3.Cross(Vector3.up, GetPathForward());
        Vector3 offset = side * (Mathf.Sin(wavePhase * Mathf.PI * 2f) * data.waveAmplitude * rise)
                       + Vector3.up * (height + Mathf.Sin(bobPhase) * data.bobHeight);

        body.position = transform.TransformPoint(bodyRestPosition) + offset;
    }

    // Path direction on the ground plane
    private Vector3 GetPathForward()
    {
        if (splineContainer == null) return Vector3.forward;

        Vector3 tangent = splineContainer.EvaluateTangent(distancePercentage);
        tangent.y = 0f;
        return tangent.sqrMagnitude > 0.0001f ? tangent.normalized : Vector3.forward;
    }
}
