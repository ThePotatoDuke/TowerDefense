using UnityEngine;
using DG.Tweening;

public class SpikeBallEnemy : EnemyBase
{
    [SerializeField] private SpikeBallDataSO data;
    [SerializeField] private Transform spikeBallVisual;

    private Vector3 lastPos;
    private float progressVelocity = 0f;     // velocity along spline
    private float currentTargetProgress = 0f;// tweened target
    private float offsetTimer = 0f;
    private int wavesSurvived = 0;

    public override EnemyDataSO Data => data;

    // Spike balls only shield other enemies: they don't hold up the next wave or hit the castle
    public override bool BlocksWaveProgress => false;
    protected override bool DamagesCastle => false;

    protected override void Awake()
    {
        base.Awake();
        lastPos = transform.position;

        // Draw the 3D ball as outlined pixel art, with a ground shadow like the sprites
        MeshRenderer model = GetComponentInChildren<MeshRenderer>(); // before the imposter adds its quad
        gameObject.GetOrAddComponent<PixelImposter>();
        gameObject.GetOrAddComponent<BlobShadow>().Source = model;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        progressVelocity = 0f;
        currentTargetProgress = distancePercentage;
        GameEvents.OnWaveCleared += HandleWaveCleared;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        GameEvents.OnWaveCleared -= HandleWaveCleared;
        DOTween.Kill(this);
    }

    private void HandleWaveCleared()
    {
        wavesSurvived++;
        if (wavesSurvived < data.wavesToSurvive) return;

        Die();
    }

    protected override void Update()
    {
        if (isDead) return;

        UpdateTargetProgress();
        Move();
        HandleRollingVisual();

        // Let EnemyBase handle the "attack castle" tick
        base.Update();
    }

    private void UpdateTargetProgress()
    {
        if (EnemyManager.Instance == null) return;

        offsetTimer -= Time.deltaTime;
        if (offsetTimer > 0f) return;

        offsetTimer = data.offsetUpdateInterval;

        float averageProgress = EnemyManager.Instance.GetAverageProgress();
        float randomOffset = Random.Range(-data.bouncyOffset, data.bouncyOffset);
        float newTarget = Mathf.Clamp01(averageProgress + randomOffset);

        DOTween.To(() => currentTargetProgress,
                   x => currentTargetProgress = x,
                   newTarget,
                   data.offsetTweenDuration)
               .SetEase(data.bouncyEase)
               .SetId(this);
    }

    public override void Move()
    {
        float dt = Time.deltaTime;

        // spring-damper toward target
        float displacement = distancePercentage - currentTargetProgress;
        float force = -data.springK * displacement;
        float damping = -data.damping * progressVelocity;
        float acceleration = force + damping;

        progressVelocity += acceleration * dt;
        progressVelocity = Mathf.Clamp(progressVelocity, -data.maxSpeed, data.maxSpeed);

        float previousProgress = distancePercentage;
        distancePercentage = Mathf.Clamp01(distancePercentage + progressVelocity * dt);

        if (splineContainer != null)
        {
            Vector3 startPos = splineContainer.EvaluatePosition(previousProgress);
            Vector3 endPos = splineContainer.EvaluatePosition(distancePercentage);
            transform.position += endPos - startPos;
        }
        else
        {
            transform.position += new Vector3(distancePercentage - previousProgress, 0f, 0f);
        }

        // End detection
        if (!hasReachedEnd && distancePercentage >= 0.999f)
            ReachEnd();
    }

    private void HandleRollingVisual()
    {
        if (spikeBallVisual == null) return;

        Vector3 movement = transform.position - lastPos;
        float distanceTraveled = movement.magnitude;

        if (distanceTraveled > 0.0001f)
        {
            Vector3 rollingAxis = Vector3.Cross(Vector3.up, movement.normalized);
            float rotationDegrees = (distanceTraveled / (2 * Mathf.PI * data.ballRadius)) * 360f;
            spikeBallVisual.Rotate(rollingAxis, rotationDegrees, Space.World);
        }

        lastPos = transform.position;
    }
}
