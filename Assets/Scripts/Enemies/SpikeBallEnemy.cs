using System.Collections;
using UnityEngine;
using DG.Tweening;

public class SpikeBallEnemy : EnemyBase
{
    [SerializeField] private SpikeBallDataSO data;
    [SerializeField] private Transform spikeBallVisual;

    private Vector3 lastPos;
    private float progressVelocity = 0f;     // speed along the path, units per second
    private float bumpVelocity = 0f;         // extra speed along the path from a roller's knock, dying away
    private float currentTargetProgress = 0f;// tweened target
    private float offsetTimer = 0f;
    private int wavesSurvived = 0;

    public override EnemyDataSO Data => data;

    // Spike balls only shield other enemies: they don't hold up the next wave or hit the castle
    public override bool BlocksWaveProgress => false;
    protected override bool DamagesCastle => false;
    // Rollers knock spike balls along the path instead of them hopping over (see TryBump)
    public override bool CanDodge => false;

    private const float BumpDecay = 3f; // how quickly a knock slows down, per second

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
        bumpVelocity = 0f;
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

    // Destroyed by damage (not by outliving its waves), a ball with a spike burst warns where the
    // spikes will fly, then fires them before it goes
    protected override float PlayDeathEffect()
    {
        bool killed = currentHealth <= 0f;
        if (!killed || data.deathSpikeCount <= 0 || data.spikeSprite == null)
            return base.PlayDeathEffect();

        StartCoroutine(SpikeBurst());
        return -1f; // destroyed after the burst
    }

    private IEnumerator SpikeBurst()
    {
        Vector3 center = spikeBallVisual != null ? spikeBallVisual.position : transform.position + Vector3.up * data.ballRadius;
        Vector3 groundPoint = new Vector3(center.x, transform.position.y, center.z);

        // Equal angles all the way round, starting at a random one
        int count = data.deathSpikeCount;
        float startAngle = Random.Range(0f, 360f);
        var lines = new Transform[count];
        var lineMaterials = new Material[count];
        var spikes = new SpikeProjectile[count];
        float spikeStart = data.ballRadius * 0.8f; // spikes wait just outside the ball, pointing out
        for (int i = 0; i < count; i++)
        {
            Vector3 direction = Quaternion.Euler(0f, startAngle + 360f * i / count, 0f) * Vector3.forward;
            lines[i] = GroundLine.Create(groundPoint, direction, data.spikeRange, data.spikeWarningColor, out lineMaterials[i]);
            spikes[i] = SpikeProjectile.Create(center + direction * spikeStart, direction, data.spikeSprite, data.spikeMaterial,
                                               data.spikeSpeed, data.spikeRange - spikeStart, data.spikeDamage);
        }

        // Warn: the spikes wait faintly at the start of their paths while dashed lines run outward along
        // them and the ball swells and flashes
        Transform ball = visual != null ? visual : transform;
        ball.DOScale(ball.localScale * 1.25f, data.spikeWarningTime).SetEase(Ease.InQuad).SetLink(gameObject);

        float elapsed = 0f;
        float nextFlash = 0f;
        while (elapsed < data.spikeWarningTime)
        {
            if (elapsed >= nextFlash)
            {
                Flash();
                nextFlash += 0.15f;
            }

            float blink = (int)(elapsed * 10f) % 2 == 0 ? 1f : 0.5f;
            foreach (Material lineMaterial in lineMaterials)
            {
                if (lineMaterial == null) continue;
                GroundLine.MarchDashes(lineMaterial, elapsed, 30f);
                lineMaterial.SetColor("_Color", data.spikeWarningColor * blink);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            if (lines[i] != null)
                Destroy(lines[i].gameObject);
            if (lineMaterials[i] != null)
                Destroy(lineMaterials[i]);
            if (spikes[i] != null)
                spikes[i].Launch();
        }

        // Then pop like any other spike ball
        ball.DOKill();
        Destroy(gameObject, PlayDefaultDeathEffect());
    }

    private float PlayDefaultDeathEffect() => base.PlayDeathEffect();

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

        // Spring-damper toward the target, in real distances along the path (units, units per second),
        // so the stiffness, damping and speed limit mean the same however long the path is
        float displacement = (distancePercentage - currentTargetProgress) * splineLength;
        float force = -data.springK * displacement;
        float damping = -data.damping * progressVelocity;
        float acceleration = force + damping;

        progressVelocity += acceleration * dt;
        progressVelocity = Mathf.Clamp(progressVelocity, -data.maxSpeed, data.maxSpeed);

        // A roller's knock rides on top of the spring and fades out
        bumpVelocity *= Mathf.Exp(-BumpDecay * dt);

        float previousProgress = distancePercentage;
        distancePercentage = Mathf.Clamp01(distancePercentage + (progressVelocity + bumpVelocity) * dt / splineLength);

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

    // Hit by a roller: knocked along the path, whichever way leads away from it
    public override bool TryBump(Vector3 away, float speed)
    {
        if (isDead) return false;

        Vector3 pathDirection = splineContainer != null ? (Vector3)splineContainer.EvaluateTangent(distancePercentage) : Vector3.right;
        pathDirection.y = 0f;
        float sign = Vector3.Dot(pathDirection, away) >= 0f ? 1f : -1f;

        progressVelocity = 0f;
        bumpVelocity = sign * speed;
        return true;
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
