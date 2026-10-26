using System.Collections.Generic;
using UnityEngine;

// Spiked cylinder that rolls back and forth along the path. Reaching either end turns it around,
// two rollers that meet bounce apart in a shower of sparks, enemies in its way hop over it, spike balls
// get bumped along, tibbits on the ground get flattened, an enemy it runs into that can't jump takes a
// little damage, and it mows the grass.
public class RollerEnemy : EnemyBase
{
    private static readonly List<RollerEnemy> activeRollers = new();

    // Slowest an enemy and the roller can close in on each other and still count as meeting
    private const float MinClosingSpeed = 0.3f;
    // Below this (sine of the angle between the enemy's path and the cylinder), it can't jump across it
    private const float MinPathCrossingAngle = 0.3f;
    // An enemy that can't jump across hops in place as the roller passes under it, if it passes at least
    // this fraction of the roller's speed (any slower would need a huge jump)
    private const float MinHopInPlaceSpeed = 0.5f;
    // Enemies further off than they could cover in this many seconds (plus the cylinder's size) are skipped
    // before any spline maths, since they can't need to jump yet
    private const float LookaheadTime = 1f;

    [SerializeField] private RollerDataSO data;
    [Tooltip("Child holding the model, centered on the cylinder. The cylinder's axis must lie along this child's local X.")]
    [SerializeField] private Transform rollerVisual;
    [Tooltip("Log each enemy the roller hurts, and why, with a red line in the Scene view.")]
    [SerializeField] private bool debugHits;

    private int direction = 1; // +1 toward the castle, -1 back toward the spawn
    private float spinAngle;
    private int wavesSurvived = 0;
    private float halfLength; // half the cylinder's length along its axis
    public float HalfLength => halfLength;
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
        HandleEnemies();
        UpdateVisual();

        // Flattens the grass it rolls over, like a sword swing does
        if (GroundDecorator.Instance != null && body != null)
            GroundDecorator.Instance.CutGrass(body);
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

            PlaySparks((GetVisualCenter() + other.GetVisualCenter()) * 0.5f);
        }
    }

    // --- Enemies meeting the roller ---
    // One check in world space for every ground enemy, whichever stretch of path it's on (the path can
    // loop past itself). On the ground the cylinder is a strip: its axis, padded by both radii across and
    // by the enemy's radius at the ends. From the enemy's and the roller's motion we work out when the
    // enemy reaches the middle of the strip, and it jumps so the top of the jump comes right then.
    // During the jump it moves along its own path fast enough to cross at a set speed, so the jump is the
    // same every time: high enough to clear the roller for the whole crossing, under normal gravity.
    // Anything already touching hops straight away. Only an enemy the roller runs into while it can't
    // jump (it only just landed) gets hurt; spike balls are bumped along instead.
    private void HandleEnemies()
    {
        if (EnemyManager.Instance == null || rollerVisual == null || splineContainer == null) return;

        Vector3 axis = FlatDirection(rollerVisual.right);
        if (axis == Vector3.zero) return;
        Vector3 across = Vector3.Cross(Vector3.up, axis); // the roller's direction of travel, flat
        Vector3 rollerMotion = FlatDirection(splineContainer.EvaluateTangent(distancePercentage)) * (direction * data.moveSpeed);

        ForgetStaleTouches();

        // Backwards and without copying the list, since a hit can kill an enemy and take it off the list
        IReadOnlyList<IEnemy> enemies = EnemyManager.Instance.ActiveEnemies;
        float closeEnough = halfLength + data.radius;
        float lookaheadSpeed = (data.moveSpeed + data.vaultSpeed) * LookaheadTime;
        for (int i = enemies.Count - 1; i >= 0; i--)
        {
            if (i >= enemies.Count) continue;
            if (enemies[i] is not EnemyBase enemy || enemy.IsDead || enemy.IsFlying || enemy is RollerEnemy) continue;

            Vector3 offset = enemy.transform.position - rollerVisual.position;
            offset.y = 0f;

            // Cheap check first: most enemies are nowhere near
            float range = closeEnough + enemy.BodyRadius + lookaheadSpeed + Mathf.Abs(enemy.PathSpeed) * LookaheadTime;
            if (offset.sqrMagnitude > range * range) continue;

            float side = Vector3.Dot(offset, across);
            float sideSign = side >= 0f ? 1f : -1f;
            float along = Vector3.Dot(offset, axis);
            float reach = data.radius + enemy.BodyRadius;
            float endReach = halfLength + enemy.BodyRadius;

            Vector3 enemyDirection = FlatDirection(splineContainer.EvaluateTangent(enemy.DistancePercentage));
            // How fast the gap across the strip closes, with both moving as they are now
            float closing = -sideSign * Vector3.Dot(enemyDirection * enemy.PathSpeed - rollerMotion, across);

            if (Mathf.Abs(side) < reach && Mathf.Abs(along) < endReach)
            {
                HandleTouch(enemy, axis, across, along, sideSign, closing, enemyDirection, rollerMotion);
                continue;
            }

            if (enemy.IsDodging || !enemy.CanDodge || closing < MinClosingSpeed) continue;
            JumpIfCrossing(enemy, offset, axis, across, side, sideSign, endReach, enemyDirection, rollerMotion);
        }
    }

    // An enemy closing in on the roller: jumps now if the top of the jump would otherwise come too late
    private void JumpIfCrossing(EnemyBase enemy, Vector3 offset, Vector3 axis, Vector3 across, float side, float sideSign,
                                float endReach, Vector3 enemyDirection, Vector3 rollerMotion)
    {
        // Leap across it along its path, or if its path runs alongside the cylinder, hop in place
        // and let the roller pass underneath
        if (!TryVaultVelocity(enemy, enemyDirection, rollerMotion, across, sideSign, out float pathVelocity))
            pathVelocity = enemy.PathSpeed;

        Vector3 relativeMotion = enemyDirection * pathVelocity - rollerMotion;
        float crossingSpeed = -sideSign * Vector3.Dot(relativeMotion, across);
        if (crossingSpeed < data.moveSpeed * MinHopInPlaceSpeed) return;
        float timeToMiddle = Mathf.Abs(side) / crossingSpeed; // until it's over the cylinder's axis

        // Where along the cylinder it crosses; past either end it just walks by
        float alongAtCrossing = Vector3.Dot(offset + relativeMotion * timeToMiddle, axis);
        if (Mathf.Abs(alongAtCrossing) > endReach) return;

        VaultHeight(data.radius + enemy.BodyRadius, crossingSpeed, out float height, out float airTime);

        // Take off this frame if the top of the jump would come after the middle by the next one
        if (timeToMiddle - Time.deltaTime > airTime * 0.5f) return;

        enemy.Dodge(height, airTime, pathVelocity);
    }

    private void HandleTouch(EnemyBase enemy, Vector3 axis, Vector3 across, float along, float sideSign, float closing,
                             Vector3 enemyDirection, Vector3 rollerMotion)
    {
        if (enemy.IsDodging) return; // passing over it

        // Tibbits are too small to hop over it: one on the ground is flattened (riding ones are up out of reach)
        if (enemy is TibbitEnemy tibbit)
        {
            tibbit.Squash();
            return;
        }

        // Spike balls get knocked along their path, with sparks where they meet
        Vector3 nearestOnAxis = rollerVisual.position + axis * along;
        Vector3 away = enemy.transform.position - nearestOnAxis;
        away.y = 0f;
        if (away.sqrMagnitude < 0.0001f)
            away = across * sideSign;
        if (!RecentlyTouched(enemy) && enemy.TryBump(away.normalized, data.moveSpeed + data.bumpSpeed))
        {
            lastTouchTimes[enemy] = Time.time;
            PlaySparks(nearestOnAxis + away.normalized * data.radius);
            return;
        }

        if (!enemy.CanDodge) return;

        // Already touching: hop over it now, the same way it would have jumped in time
        if (!TryVaultVelocity(enemy, enemyDirection, rollerMotion, across, sideSign, out float pathVelocity))
            pathVelocity = enemy.PathSpeed;
        float crossingSpeed = Mathf.Max(Mathf.Abs(Vector3.Dot(enemyDirection * pathVelocity - rollerMotion, across)),
                                        data.moveSpeed * MinHopInPlaceSpeed);
        VaultHeight(data.radius + enemy.BodyRadius, crossingSpeed, out float height, out float airTime);
        if (enemy.Dodge(height, airTime, pathVelocity)) return;

        // It couldn't jump. Only hurt it if the roller is actually running into it, not just brushing past
        if (closing < MinClosingSpeed || RecentlyTouched(enemy)) return;

        lastTouchTimes[enemy] = Time.time;
        enemy.TakeDamage(data.touchDamage);

        if (debugHits)
        {
            string reason = enemy.JustLanded ? "it had only just landed" : "it has nothing to lift (no jump root)";
            Debug.Log($"{name} hit {enemy.name}: {reason}", enemy);
            Debug.DrawLine(rollerVisual.position, enemy.transform.position + Vector3.up, Color.red, 1f);
        }
    }

    // How fast the enemy moves along its own path during the vault, so it crosses the roller at least at
    // the usual vault speed. False if its path runs nearly along the cylinder: no jump along it would cross.
    private bool TryVaultVelocity(EnemyBase enemy, Vector3 enemyDirection, Vector3 rollerMotion, Vector3 across,
                                  float sideSign, out float pathVelocity)
    {
        pathVelocity = enemy.PathSpeed;
        float crossingSpeed = data.moveSpeed + data.vaultSpeed;

        // The closing speed is -sideSign * (pathVelocity * a - b)
        float a = Vector3.Dot(enemyDirection, across);
        float b = Vector3.Dot(rollerMotion, across);
        if (Mathf.Abs(a) < MinPathCrossingAngle) return false;

        if (-sideSign * (pathVelocity * a - b) < crossingSpeed)
            pathVelocity = (b - sideSign * crossingSpeed) / a;
        return true;
    }

    // Keeps the enemy's feet above the roller's top for the whole time it's over it, crossing at this speed
    private void VaultHeight(float reach, float crossingSpeed, out float height, out float airTime)
    {
        float clearHeight = 2f * data.radius + data.vaultClearance;
        float overlapHalfTime = reach / Mathf.Max(crossingSpeed, 0.01f);
        height = clearHeight + 0.5f * data.vaultGravity * overlapHalfTime * overlapHalfTime;
        airTime = 2f * Mathf.Sqrt(2f * height / data.vaultGravity);
    }

    private bool RecentlyTouched(EnemyBase enemy) =>
        lastTouchTimes.TryGetValue(enemy, out float lastTouch) && Time.time - lastTouch < data.touchDamageInterval;

    // Forget enemies that died or haven't been touched for a while
    private void ForgetStaleTouches()
    {
        if (lastTouchTimes.Count <= 32) return;

        var stale = new List<EnemyBase>();
        foreach (var entry in lastTouchTimes)
        {
            if (entry.Key == null || Time.time - entry.Value > data.touchDamageInterval)
                stale.Add(entry.Key);
        }
        foreach (EnemyBase enemy in stale)
            lastTouchTimes.Remove(enemy);
    }

    // Sparks a bit toward the camera, so the roller itself doesn't hide them
    private void PlaySparks(Vector3 position)
    {
        if (Camera.main != null)
            position += (Camera.main.transform.position - position).normalized * data.radius * 2f;
        SparkBurst.Play(position);
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
