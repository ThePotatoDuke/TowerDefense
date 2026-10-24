using System.Collections.Generic;
using UnityEngine;

// Spiked cylinder that rolls back and forth along the path. Reaching either end turns it around,
// two rollers that meet bounce apart in a shower of sparks, and enemies in its way hop over it.
public class RollerEnemy : EnemyBase
{
    private static readonly List<RollerEnemy> activeRollers = new();

    [SerializeField] private RollerDataSO data;
    [Tooltip("Child holding the model, centered on the cylinder. The cylinder's axis must lie along this child's local X.")]
    [SerializeField] private Transform rollerVisual;

    private int direction = 1; // +1 toward the castle, -1 back toward the spawn
    private float spinAngle;
    private int wavesSurvived = 0;

    public override EnemyDataSO Data => data;

    // Rollers are a hazard, not an attacker: they don't hold up the next wave or hit the castle
    public override bool BlocksWaveProgress => false;
    protected override bool DamagesCastle => false;
    public override bool CanDodge => false;

    protected override void Awake()
    {
        base.Awake();

        // Draw the 3D cylinder as outlined pixel art, with a ground shadow like the sprites
        MeshRenderer model = GetComponentInChildren<MeshRenderer>(); // before the imposter adds its quad
        gameObject.GetOrAddComponent<PixelImposter>();
        gameObject.GetOrAddComponent<BlobShadow>().Source = model;
    }

    protected override void OnEnable()
    {
        base.OnEnable();
        activeRollers.Add(this);
        GameEvents.OnWaveCleared += HandleWaveCleared;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        activeRollers.Remove(this);
        GameEvents.OnWaveCleared -= HandleWaveCleared;
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

        base.Update(); // follow the spline
        Move();
        BounceOffOtherRollers();
        WarnEnemiesAhead();
        UpdateVisual();
    }

    public override void Move()
    {
        if (splineContainer == null) return;

        distancePercentage += direction * data.moveSpeed * Time.deltaTime / splineLength;

        // Turn around at the castle and at the spawn
        if (distancePercentage >= 1f)
        {
            distancePercentage = 1f;
            direction = -1;
        }
        else if (distancePercentage <= 0f)
        {
            distancePercentage = 0f;
            direction = 1;
        }
    }

    private void BounceOffOtherRollers()
    {
        foreach (RollerEnemy other in activeRollers)
        {
            // Each pair is handled once, by the roller that comes first in the list
            if (other == this || other.isDead || activeRollers.IndexOf(other) < activeRollers.IndexOf(this)) continue;

            // Distance along the path; positive when the other roller is further along
            float gap = (other.distancePercentage - distancePercentage) * splineLength;
            if (Mathf.Abs(gap) > data.radius + other.data.radius) continue;

            // Only bounce while closing in, so overlapping rollers don't flip back and forth every frame
            float side = gap >= 0f ? 1f : -1f;
            float closingSpeed = (direction * data.moveSpeed - other.direction * other.data.moveSpeed) * side;
            if (closingSpeed <= 0f) continue;

            // Whoever was heading into the other turns around
            if (direction == (int)side)
                direction = -direction;
            if (other.direction == -(int)side)
                other.direction = -other.direction;

            SparkBurst.Play((GetVisualCenter() + other.GetVisualCenter()) * 0.5f);
        }
    }

    // Enemies just ahead of the roller hop so they're at the top of the jump when it passes underneath
    private void WarnEnemiesAhead()
    {
        if (EnemyManager.Instance == null) return;

        float reach = data.moveSpeed * data.dodgeJumpDuration * 0.5f + data.radius;
        foreach (IEnemy candidate in EnemyManager.Instance.ActiveEnemies)
        {
            if (candidate is not EnemyBase enemy || enemy.IsDead || !enemy.CanDodge) continue;

            float ahead = (enemy.DistancePercentage - distancePercentage) * splineLength * direction;
            if (ahead > -data.radius && ahead < reach)
                enemy.Dodge(data.dodgeJumpHeight, data.dodgeJumpDuration);
        }
    }

    // Face along the path and spin around the cylinder's axis to match the distance rolled
    private void UpdateVisual()
    {
        if (rollerVisual == null || splineContainer == null) return;

        Vector3 forward = splineContainer.EvaluateTangent(distancePercentage);
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f) return;

        spinAngle += direction * data.moveSpeed * Time.deltaTime / Mathf.Max(0.01f, data.radius) * Mathf.Rad2Deg;
        spinAngle = Mathf.Repeat(spinAngle, 360f);
        rollerVisual.rotation = Quaternion.LookRotation(forward.normalized) * Quaternion.Euler(spinAngle, 0f, 0f);
    }

    private Vector3 GetVisualCenter() => rollerVisual != null ? rollerVisual.position : transform.position;
}
