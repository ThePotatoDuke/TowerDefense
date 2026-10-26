using System.Collections.Generic;
using UnityEngine;

// Little frog that rides on other enemies' heads, working its way forward: it jumps onto the next enemy
// ahead of it along the path that it can reach, rides it for a while, then jumps on to the next one ahead
// (never back). It won't ride rollers, spike balls, other tibbits or a Molly that's underground, and gets
// off a Molly the moment she ducks. Riding, it can't be hit; off a ride (on the ground or mid-jump) it can.
// Near the castle it leaps off at the castle and attacks it. Rollers flatten it when it's on the ground.
// The data decides the rest: whether it dies with its ride or falls off dazed and carries on alone, how
// fast it goes alone, and whether it jumps off to run for the castle when nobody ahead is in reach.
public class TibbitEnemy : EnemyBase
{
    private enum State { Ground, Dazed, Jumping, Riding, AtCastle }

    // Enemies that already carry (or are about to carry) a tibbit: one each
    private static readonly HashSet<EnemyBase> ridden = new();

    [SerializeField] private TibbitDataSO data;

    private State state = State.Ground;
    private EnemyBase host;        // the enemy it rides, or is jumping onto
    private Transform perch;       // where on the host it sits
    private Vector3 perchLocal;    // ...in that transform's space
    private float rideTimer;
    private float stuckTimer;      // how long nobody ahead has been in reach, once its ride time is up
    private float searchTimer;
    private float dazeTimer;
    private Vector3 jumpStart;
    private float jumpTime;
    private float jumpDuration;
    private float jumpHeightScale = 1f;
    private bool jumpingAtCastle;
    private bool dieOnLanding;     // falling off a ride that died, and dying with it
    private bool dazeOnLanding;    // falling off a ride that died, and carrying on after a moment
    private SpriteRenderer sprite;
    private Vector3 spriteRest;
    private Collider[] colliders;
    private float animationTime;
    private float hopPhase;

    public override EnemyDataSO Data => data;
    // Anything but riding: it can be hit
    public bool IsVulnerable => state != State.Riding;
    // On the ground, where rollers flatten it
    public bool IsOnGround => state == State.Ground || state == State.Dazed || state == State.AtCastle;
    // Too small to hop over a roller
    public override bool CanDodge => false;

    protected override void Awake()
    {
        base.Awake();
        ridden.RemoveWhere(enemy => enemy == null);

        sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite != null)
            spriteRest = sprite.transform.localPosition;
        colliders = GetComponentsInChildren<Collider>(true);
        SetHittable(true);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        LeaveHost();
    }

    public override void Move() { } // each state moves it in Update / LateUpdate

    protected override void Update()
    {
        if (isDead) return;

        switch (state)
        {
            case State.Ground:
                UpdateGround();
                break;
            case State.Dazed:
                UpdateDazed();
                break;
            case State.Riding:
                UpdateRiding();
                break;
            case State.AtCastle:
                base.Update(); // stands at the end of the path, hitting the castle
                break;
        }
        Animate();
    }

    // Follows its ride after the ride has moved this frame, so it doesn't lag a frame behind
    protected override void LateUpdate()
    {
        if (!isDead)
        {
            if (state == State.Riding && perch != null)
                transform.position = PerchPoint();
            else if (state == State.Jumping)
                UpdateJump();
        }
        base.LateUpdate();
    }

    public override void TakeDamage(float amount)
    {
        if (!IsVulnerable) return;
        base.TakeDamage(amount);
    }

    // A roller ran it over
    public void Squash()
    {
        if (IsOnGround)
            Die();
    }

    protected override void Die()
    {
        LeaveHost();
        base.Die();
    }

    // --- States ---

    // Alone on the path: hops forward, and jumps onto the next enemy ahead the moment one is in reach
    private void UpdateGround()
    {
        bool nearCastle = (1f - distancePercentage) * splineLength <= data.castleLeapDistance;

        searchTimer -= Time.deltaTime;
        if (!nearCastle && searchTimer <= 0f)
        {
            searchTimer = data.searchInterval;
            EnemyBase next = FindHost();
            if (next != null)
            {
                JumpOnto(next);
                return;
            }
        }

        distancePercentage = Mathf.Min(1f, distancePercentage + data.groundSpeed * Time.deltaTime / splineLength);
        base.Update();

        if (hasReachedEnd)
            ArriveAtCastle();
    }

    // Sitting stunned where it fell, then off again on its own
    private void UpdateDazed()
    {
        base.Update(); // stays put on the path

        dazeTimer -= Time.deltaTime;
        if (dazeTimer > 0f) return;

        state = State.Ground;
        searchTimer = 0f;
    }

    private void UpdateRiding()
    {
        // Molly ducked, or the ride can't be ridden any more for another reason: on to the next one ahead,
        // or down onto the path
        if (!IsValidHost(host))
        {
            LeaveHost();
            EnemyBase next = FindHost();
            if (next != null)
                JumpOnto(next);
            else
                JumpToGround();
            return;
        }

        distancePercentage = host.DistancePercentage; // where it would land if it fell off

        if (host.HasReachedEnd || (1f - host.DistancePercentage) * splineLength <= data.castleLeapDistance)
        {
            LeapAtCastle();
            return;
        }

        rideTimer -= Time.deltaTime;
        if (rideTimer > 0f) return;

        // Time to move on: to the next one ahead, or (if it runs when stuck) off on its own after a while
        stuckTimer += Time.deltaTime;
        searchTimer -= Time.deltaTime;
        if (searchTimer > 0f) return;
        searchTimer = data.searchInterval;

        EnemyBase other = FindHost();
        if (other != null)
            JumpOnto(other);
        else if (data.runsWhenStuck && stuckTimer >= data.stuckTime)
            JumpToGround();
    }

    private void UpdateJump()
    {
        // The enemy it was jumping onto died or ducked mid-air: aim for the next one ahead, or the ground
        if (!jumpingAtCastle && host != null && !IsValidHost(host))
        {
            LeaveHost();
            EnemyBase next = FindHost();
            if (next != null)
                Claim(next);
        }

        jumpTime += Time.deltaTime;
        float t = Mathf.Clamp01(jumpTime / jumpDuration);
        Vector3 end = JumpTarget();
        transform.position = Vector3.Lerp(jumpStart, end, t) + Vector3.up * (data.jumpHeight * jumpHeightScale * 4f * t * (1f - t));

        if (t >= 1f)
            Land();
    }

    private void Land()
    {
        if (dieOnLanding)
        {
            Die();
            return;
        }

        if (jumpingAtCastle)
        {
            distancePercentage = 1f;
            ArriveAtCastle();
        }
        else if (host != null)
        {
            state = State.Riding;
            host.OnDied += HandleHostDied;
            rideTimer = Random.Range(data.minRideTime, data.maxRideTime);
            stuckTimer = 0f;
            searchTimer = 0f;
            distancePercentage = host.DistancePercentage;
            SetHittable(false);
        }
        else if (dazeOnLanding)
        {
            dazeOnLanding = false;
            state = State.Dazed;
            dazeTimer = data.dazeTime;
        }
        else
        {
            state = State.Ground;
            searchTimer = data.searchInterval;
        }
    }

    private void ArriveAtCastle()
    {
        state = State.AtCastle;
        jumpingAtCastle = false;
    }

    // --- Jumping ---

    private void JumpOnto(EnemyBase target)
    {
        LeaveHost();
        Claim(target);
        jumpingAtCastle = false;
        StartJump();
    }

    private void LeapAtCastle()
    {
        LeaveHost();
        jumpingAtCastle = true;
        StartJump();
    }

    private void JumpToGround()
    {
        LeaveHost();
        jumpingAtCastle = false;
        StartJump();
    }

    private void StartJump(float heightScale = 1f)
    {
        state = State.Jumping;
        SetHittable(true); // not riding any more
        jumpStart = transform.position;
        jumpTime = 0f;
        jumpHeightScale = heightScale;

        Vector3 end = JumpTarget();
        Vector3 across = end - jumpStart;
        across.y = 0f;
        jumpDuration = Mathf.Clamp(across.magnitude / Mathf.Max(0.01f, data.jumpSpeed), data.minJumpTime, data.maxJumpTime);
        Face(end - jumpStart);
    }

    // Its ride died under it: a little hop off onto the path, then it dies there or sits dazed
    private void FallOff()
    {
        LeaveHost();
        jumpingAtCastle = false;
        dieOnLanding = data.diesWithHost;
        dazeOnLanding = !data.diesWithHost;
        StartJump(0.35f);
    }

    // Where the jump lands: the ride's head, the castle, or the path below
    private Vector3 JumpTarget()
    {
        if (jumpingAtCastle)
            return PathPoint(1f);
        if (host != null && perch != null)
            return PerchPoint();
        return PathPoint(distancePercentage);
    }

    private Vector3 PathPoint(float progress) =>
        splineContainer != null ? (Vector3)splineContainer.EvaluatePosition(progress) : transform.position;

    // --- Rides ---

    // The next enemy ahead of it along the path that it could ride and can reach; none if nobody ahead is
    // in reach (it never jumps back)
    private EnemyBase FindHost()
    {
        if (EnemyManager.Instance == null) return null;

        float from = host != null ? host.DistancePercentage : distancePercentage;
        EnemyBase next = null;
        float nextGap = float.MaxValue;

        foreach (IEnemy candidate in EnemyManager.Instance.ActiveEnemies)
        {
            if (candidate is not EnemyBase enemy || enemy == host || !IsValidHost(enemy)) continue;

            Vector3 offset = enemy.transform.position - transform.position;
            offset.y = 0f;
            if (offset.magnitude > data.jumpRange) continue;

            float gap = enemy.DistancePercentage - from;
            if (gap > 0f && gap < nextGap)
            {
                next = enemy;
                nextGap = gap;
            }
        }
        return next;
    }

    private bool IsValidHost(EnemyBase enemy)
    {
        if (enemy == null || enemy == this || enemy.IsDead || enemy.HasReachedEnd) return false;
        if (enemy is TibbitEnemy || enemy is RollerEnemy || enemy is SpikeBallEnemy) return false;
        if (enemy is MollyEnemy molly && !molly.IsExposed) return false; // underground
        if (enemy.MainRenderer is not SpriteRenderer) return false;
        return enemy == host || !ridden.Contains(enemy);
    }

    // Takes the enemy as its ride (so no other tibbit picks it) and finds where to sit: its TibbitPerch
    // if it has one, otherwise the top middle of its sprite
    private void Claim(EnemyBase target)
    {
        host = target;
        ridden.Add(target);

        TibbitPerch marker = target.GetComponentInChildren<TibbitPerch>(true);
        if (marker != null)
        {
            perch = marker.transform;
            perchLocal = Vector3.zero;
            return;
        }

        var hostSprite = (SpriteRenderer)target.MainRenderer;
        perch = hostSprite.transform;
        Bounds local = hostSprite.localBounds;
        perchLocal = new Vector3(local.center.x, local.max.y, 0f);
    }

    private void LeaveHost()
    {
        if (host == null) return;
        host.OnDied -= HandleHostDied;
        ridden.Remove(host);
        host = null;
        perch = null;
    }

    private void HandleHostDied() => FallOff();

    private Vector3 PerchPoint() => perch.TransformPoint(perchLocal);

    // --- Looks ---

    // Idle frames; the hop frame in the air, including the little hops it moves in on the ground
    private void Animate()
    {
        if (sprite == null) return;

        float hop = 0f;
        if (state == State.Ground)
        {
            hopPhase += Time.deltaTime * data.groundHopsPerSecond;
            hop = Mathf.Abs(Mathf.Sin(hopPhase * Mathf.PI)) * data.groundHopHeight;
            Face(PathPoint(Mathf.Min(1f, distancePercentage + 0.01f)) - transform.position);
        }
        else
        {
            hopPhase = 0f;
        }
        sprite.transform.localPosition = spriteRest + Vector3.up * hop;

        bool inAir = state == State.Jumping || hop > data.groundHopHeight * 0.25f;
        if (inAir && data.hopSprite != null)
        {
            sprite.sprite = data.hopSprite;
            return;
        }

        if (data.idleFrames == null || data.idleFrames.Length == 0) return;
        animationTime += Time.deltaTime;
        int frame = (int)(animationTime * data.idleFramesPerSecond) % data.idleFrames.Length;
        sprite.sprite = data.idleFrames[frame];
    }

    // The frames face left, so it's flipped to face right
    private void Face(Vector3 direction)
    {
        Camera view = Camera.main;
        if (sprite == null || view == null) return;

        float sideways = Vector3.Dot(direction, view.transform.right);
        if (Mathf.Abs(sideways) > 0.01f)
            sprite.flipX = sideways > 0f;
    }

    // Colliders are what weapons find and projectiles hit, so they're off while it rides
    private void SetHittable(bool hittable)
    {
        foreach (Collider body in colliders)
            if (body != null)
                body.enabled = hittable;
    }
}
