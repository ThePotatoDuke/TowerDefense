using UnityEngine;
using DG.Tweening;

public class SpikeBallEnemy : EnemyBase
{
    [SerializeField] private SpikeBallDataSO data;
    [SerializeField] private Transform spikeBallVisual;

    private Vector3 lastPos;
    private float progress = 0f;             // current spline progress
    private float progressVelocity = 0f;     // velocity along spline
    private float currentTargetProgress = 0f;// tweened target
    private float offsetTimer = 0f;

    public override EnemyDataSO Data => data;

    protected override void Awake()
    {
        base.Awake();
        lastPos = transform.position; // initialize lastPos
    }

    protected override void OnEnable()
    {
        // EnemyManager.Instance?.RegisterEnemy(this); // optional
    }

    protected override void OnDisable()
    {
    }

    protected override void Update()
    {
        base.Update();
        UpdateTargetProgress();
        Move();
        HandleRollingVisual();
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

        // Tween smoothly to the new target progress
        DOTween.To(() => currentTargetProgress, x => currentTargetProgress = x, newTarget, data.offsetTweenDuration)
               .SetEase(data.bouncyEase)
               .SetId(this);
    }

    public override void Move()
    {
        float dt = Time.deltaTime;

        // spring movement toward the current target
        float displacement = progress - currentTargetProgress;
        float force = -data.springK * displacement;
        float damping = -data.damping * progressVelocity;
        float acceleration = force + damping;

        progressVelocity += acceleration * dt;
        progressVelocity = Mathf.Clamp(progressVelocity, -data.maxSpeed, data.maxSpeed);

        float previousProgress = progress;
        progress += progressVelocity * dt;

        if (splineContainer != null)
        {
            Vector3 startPos = splineContainer.EvaluatePosition(previousProgress);
            Vector3 endPos = splineContainer.EvaluatePosition(progress);
            transform.position += endPos - startPos;
        }
        else
        {
            transform.position += new Vector3(progress - previousProgress, 0f, 0f);
        }
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
