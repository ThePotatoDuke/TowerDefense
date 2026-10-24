using System.Collections.Generic;
using UnityEngine;

// Spiked cylinder that rolls back and forth along the path. Reaching either end turns it around,
// two rollers that meet bounce apart in a shower of sparks, enemies in its way hop over it,
// ground enemies that still end up touching it take a little damage, and it mows the grass.
public class RollerEnemy : EnemyBase
{
    private static readonly List<RollerEnemy> activeRollers = new();

    // Extra path distance beyond the vault range before an enemy counts as being on another stretch of path
    private const float CrossingPathGap = 1f;
    // Slowest an enemy can close in on the cylinder's line and still be treated as crossing it
    private const float MinCrossingSpeed = 1f;

    [SerializeField] private RollerDataSO data;
    [Tooltip("Child holding the model, centered on the cylinder. The cylinder's axis must lie along this child's local X.")]
    [SerializeField] private Transform rollerVisual;

    private int direction = 1; // +1 toward the castle, -1 back toward the spawn
    private float spinAngle;
    private int wavesSurvived = 0;
    private float halfLength; // half the cylinder's length along its axis
    private CapsuleCollider body;
    private readonly Dictionary<EnemyBase, float> lastTouchTimes = new();

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

        // The capsule collider runs along the axis and spans the whole cylinder
        body = rollerVisual != null ? rollerVisual.GetComponent<CapsuleCollider>() : null;
        halfLength = body != null ? body.height * 0.5f * rollerVisual.lossyScale.x : data.radius;
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
        MakeEnemiesVault();
        DamageTouchingEnemies();
        UpdateVisual();

        // Flattens the grass it rolls over, like a sword swing does
        if (GroundDecorator.Instance != null && body != null)
            GroundDecorator.Instance.CutGrass(body);
    }

    // Ground enemies touching the cylinder's body take a little damage, at most once per interval each.
    // Measured in world space against the whole length of the cylinder, so it also catches enemies on
    // another stretch of the path that the roller sweeps across (e.g. where the path loops past itself).
    private void DamageTouchingEnemies()
    {
        if (EnemyManager.Instance == null || rollerVisual == null) return;

        Vector3 axis = rollerVisual.right;
        axis.y = 0f;
        if (axis.sqrMagnitude < 0.0001f) return;
        axis.Normalize();

        // Forget enemies that died or haven't been hit for a while
        if (lastTouchTimes.Count > 32)
        {
            var stale = new List<EnemyBase>();
            foreach (var entry in lastTouchTimes)
            {
                if (entry.Key == null || Time.time - entry.Value > data.touchDamageInterval)
                    stale.Add(entry.Key);
            }
            foreach (EnemyBase enemy in stale)
                lastTouchTimes.Remove(enemy);
        }

        foreach (IEnemy candidate in EnemyManager.Instance.GetAllEnemies())
        {
            // Only enemies on the ground: not flyers, other rollers, or anything mid-hop over it
            if (candidate is not EnemyBase enemy || enemy.IsDead || !enemy.CanDodge || enemy.IsDodging) continue;

            // Flat distance from the enemy to the nearest point on the cylinder's axis
            Vector3 offset = enemy.transform.position - rollerVisual.position;
            offset.y = 0f;
            float along = Mathf.Clamp(Vector3.Dot(offset, axis), -halfLength, halfLength);
            if ((offset - axis * along).magnitude > data.radius + enemy.BodyRadius) continue;

            if (lastTouchTimes.TryGetValue(enemy, out float lastTouch) && Time.time - lastTouch < data.touchDamageInterval)
                continue;

            lastTouchTimes[enemy] = Time.time;
            enemy.TakeDamage(data.touchDamage);
        }
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

            // Burst a bit toward the camera, so the rollers themselves don't hide it
            Vector3 impact = (GetVisualCenter() + other.GetVisualCenter()) * 0.5f;
            if (Camera.main != null)
                impact += (Camera.main.transform.position - impact).normalized * data.radius * 2f;
            SparkBurst.Play(impact);
        }
    }

    // --- Making enemies vault over the roller ---
    // Everything is measured along the path. An enemy in the roller's way jumps toward and across it, so
    // during the jump the pair's relative speed is always the roller's speed plus the vault speed. That
    // makes the jump the same every time: high enough to clear the roller while they overlap, under
    // normal gravity. The enemy takes off when the gap is half the distance they cover relative to each
    // other in the air, so the roller passes under the top of the jump.
    // Where the path loops over itself, an enemy on another stretch can be right next to the roller in the
    // world while far from it along the path; those are handled in world space by VaultAtCrossing.
    private void MakeEnemiesVault()
    {
        if (EnemyManager.Instance == null) return;

        float rollerVelocity = direction * data.moveSpeed;
        float crossingSpeed = data.moveSpeed + data.vaultSpeed;

        foreach (IEnemy candidate in EnemyManager.Instance.ActiveEnemies)
        {
            if (candidate is not EnemyBase enemy || enemy.IsDead || !enemy.CanDodge || enemy.IsDodging) continue;

            // Gap from roller to enemy (+ when the enemy is further along) and how fast it changes right now
            float gap = (enemy.DistancePercentage - distancePercentage) * splineLength;
            float relativeVelocity = enemy.PathSpeed - rollerVelocity;
            float reach = data.radius + enemy.BodyRadius;

            // Feet above the roller's top for the whole time the two overlap
            float clearHeight = 2f * data.radius + data.vaultClearance;
            float overlapHalfTime = reach / crossingSpeed;
            float height = clearHeight + 0.5f * data.vaultGravity * overlapHalfTime * overlapHalfTime;
            float airTime = 2f * Mathf.Sqrt(2f * height / data.vaultGravity);
            float takeoffGap = crossingSpeed * airTime * 0.5f;

            bool touching = Mathf.Abs(gap) < reach;
            bool closing = gap * relativeVelocity < 0f;
            // Take off this frame if the gap would be inside the takeoff distance by the next one
            bool inRange = Mathf.Abs(gap) - Mathf.Abs(relativeVelocity) * Time.deltaTime <= takeoffGap;
            if (touching || (closing && inRange))
            {
                // Leap across toward the other side of the roller
                float side = gap >= 0f ? 1f : -1f;
                enemy.Dodge(height, airTime, rollerVelocity - side * crossingSpeed);
            }
            else if (Mathf.Abs(gap) > reach + takeoffGap + CrossingPathGap)
            {
                // Clearly on another stretch of path, so only a crossing can bring them together
                VaultAtCrossing(enemy, rollerVelocity);
            }
        }
    }

    // An enemy on another stretch of path, about to cross the roller where the path crosses itself.
    // Measured in world space from both their directions of travel: the enemy hops so the top of its jump
    // comes as it passes over the cylinder, and keeps going along its own path at its own speed meanwhile.
    private void VaultAtCrossing(EnemyBase enemy, float rollerVelocity)
    {
        if (rollerVisual == null || splineContainer == null) return;

        Vector3 axis = FlatDirection(rollerVisual.right);
        if (axis == Vector3.zero) return;
        Vector3 across = Vector3.Cross(Vector3.up, axis); // the roller's direction of travel, flat

        // Velocity of the enemy relative to the roller, on the ground plane
        Vector3 rollerMotion = FlatDirection(splineContainer.EvaluateTangent(distancePercentage)) * rollerVelocity;
        Vector3 enemyMotion = FlatDirection(splineContainer.EvaluateTangent(enemy.DistancePercentage)) * enemy.PathSpeed;
        Vector3 relativeMotion = enemyMotion - rollerMotion;

        Vector3 offset = enemy.transform.position - rollerVisual.position;
        offset.y = 0f;

        // How fast it closes in on the cylinder's line, and when it gets there
        float side = Vector3.Dot(offset, across);
        float closingSpeed = -Mathf.Sign(side) * Vector3.Dot(relativeMotion, across);
        // Sliding along the cylinder rather than across it: no jump would clear it, so don't try
        if (closingSpeed < MinCrossingSpeed) return;
        float timeToCross = Mathf.Abs(side) / closingSpeed;

        // Where along the cylinder it crosses; past either end it just walks by
        float along = Vector3.Dot(offset + relativeMotion * timeToCross, axis);
        if (Mathf.Abs(along) > halfLength + enemy.BodyRadius) return;

        // Never higher than the usual vault: a slow, shallow crossing would otherwise need a huge jump
        float jumpSpeed = Mathf.Max(closingSpeed, data.moveSpeed + data.vaultSpeed);
        float reach = data.radius + enemy.BodyRadius;
        float clearHeight = 2f * data.radius + data.vaultClearance;
        float overlapHalfTime = reach / jumpSpeed;
        float height = clearHeight + 0.5f * data.vaultGravity * overlapHalfTime * overlapHalfTime;
        float airTime = 2f * Mathf.Sqrt(2f * height / data.vaultGravity);

        // Take off this frame if the crossing would be within half the air time by the next one
        if (timeToCross - Time.deltaTime > airTime * 0.5f) return;

        enemy.Dodge(height, airTime, enemy.PathSpeed);
    }

    // Flat distance from a point to the surface of the cylinder (its axis clamped to its length, minus
    // its radius). World space, so it also works across the loop where the path passes over itself.
    public float DistanceTo(Vector3 point)
    {
        Vector3 center = rollerVisual != null ? rollerVisual.position : transform.position;
        Vector3 offset = point - center;
        offset.y = 0f;

        Vector3 axis = rollerVisual != null ? FlatDirection(rollerVisual.right) : Vector3.zero;
        if (axis == Vector3.zero)
            return offset.magnitude - data.radius;

        float along = Mathf.Clamp(Vector3.Dot(offset, axis), -halfLength, halfLength);
        return (offset - axis * along).magnitude - data.radius;
    }

    private static Vector3 FlatDirection(Vector3 direction)
    {
        direction.y = 0f;
        return direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector3.zero;
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
