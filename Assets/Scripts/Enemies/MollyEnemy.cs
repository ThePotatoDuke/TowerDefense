using System.Collections;
using UnityEngine;

// A mole that tunnels along the path. Underground (a moving dirt mound) it can't be targeted or hurt;
// it stops, peeks out, idles for a moment, then ducks back down and digs on. It can only be hit while
// it's above ground. At the castle it comes up and stays up, attacking it.
public class MollyEnemy : EnemyBase
{
    [SerializeField] private MollyDataSO data;

    private SpriteRenderer sprite;
    private Collider[] colliders;
    private Sprite[] frames;
    private float framesPerSecond;
    private float frameClock;
    private bool loopFrames;
    private bool digging;
    private bool exposed;

    public override EnemyDataSO Data => data;

    // Only above ground can it be hit, and it never hops over rollers (it ducks under them instead)
    public bool IsExposed => exposed;
    public override bool CanDodge => false;

    protected override void Awake()
    {
        base.Awake();
        sprite = GetComponentInChildren<SpriteRenderer>();
        colliders = GetComponentsInChildren<Collider>();
    }

    private void Start()
    {
        StartCoroutine(Cycle());
    }

    protected override void Update()
    {
        if (isDead) return;

        base.Update(); // follow the spline, castle damage
        if (digging)
            Move();

        AnimateFrames();
    }

    public override void Move()
    {
        if (hasReachedEnd || splineContainer == null) return;

        float step = data.digSpeed * Time.deltaTime / splineLength;
        distancePercentage = Mathf.Clamp01(distancePercentage + step);
    }

    public override void TakeDamage(float amount)
    {
        if (!exposed) return; // safe underground, even from explosions
        base.TakeDamage(amount);
    }

    protected override void Die()
    {
        StopAllCoroutines();
        digging = false;
        base.Die();
    }

    // Dig, pop up, idle, duck down, repeat. At the castle it comes up and stays there.
    // Whenever a roller comes close it ducks (or stays) underground until the roller has gone.
    private IEnumerator Cycle()
    {
        while (true)
        {
            SetExposed(false);
            digging = true;
            PlayFrames(data.digFrames, data.digFramesPerSecond, true);

            float digEnd = Time.time + Random.Range(data.minDigTime, data.maxDigTime);
            while (Time.time < digEnd && !hasReachedEnd)
                yield return null;
            digging = false;

            yield return WaitOutRollers();
            yield return ComeUp();

            // Idle above ground, ducking whenever a roller comes by. At the castle it stays up for good.
            float idleEnd = Time.time + Random.Range(data.minIdleTime, data.maxIdleTime);
            while (hasReachedEnd || Time.time < idleEnd)
            {
                if (IsRollerNear())
                {
                    yield return Duck();
                    yield return WaitOutRollers();
                    yield return ComeUp();
                }
                yield return null;
            }

            yield return Duck();
        }
    }

    // Peeking out: already in reach from the first frame of coming up
    private IEnumerator ComeUp()
    {
        SetExposed(true);
        yield return PlayOnce(data.peekFrames, data.peekFramesPerSecond, false);
        PlayFrames(data.idleFrames, data.idleFramesPerSecond, true);
    }

    // Ducking back down, still in reach until it's gone
    private IEnumerator Duck()
    {
        yield return PlayOnce(data.peekFrames, data.peekFramesPerSecond, true);
        SetExposed(false);
    }

    // Stays buried, as a still mound of dirt, while any roller is close, then a moment longer
    private IEnumerator WaitOutRollers()
    {
        if (!IsRollerNear()) yield break;

        frames = null;
        if (data.peekFrames != null && data.peekFrames.Length > 0)
            sprite.sprite = data.peekFrames[0];

        while (IsRollerNear())
            yield return null;
        yield return new WaitForSeconds(data.resurfaceDelay);
    }

    private bool IsRollerNear()
    {
        if (EnemyManager.Instance == null) return false;

        foreach (IEnemy enemy in EnemyManager.Instance.ActiveEnemies)
        {
            if (enemy is RollerEnemy roller && !roller.IsDead
                && roller.DistanceTo(transform.position) < data.rollerDangerDistance)
                return true;
        }
        return false;
    }

    // Underground it has no collider, so weapons don't target it and nothing touches it
    private void SetExposed(bool value)
    {
        exposed = value;
        foreach (Collider col in colliders)
            col.enabled = value;
    }

    // --- Frame animation ---

    private void PlayFrames(Sprite[] newFrames, float fps, bool loop)
    {
        frames = newFrames;
        framesPerSecond = fps;
        loopFrames = loop;
        frameClock = 0f;
        ShowFrame();
    }

    private IEnumerator PlayOnce(Sprite[] newFrames, float fps, bool reverse)
    {
        if (newFrames == null || newFrames.Length == 0) yield break;

        frames = null; // stop the looping animation
        float frameTime = 1f / Mathf.Max(0.01f, fps);
        for (int i = 0; i < newFrames.Length; i++)
        {
            sprite.sprite = newFrames[reverse ? newFrames.Length - 1 - i : i];
            yield return new WaitForSeconds(frameTime);
        }
    }

    private void AnimateFrames()
    {
        if (frames == null || frames.Length == 0) return;

        frameClock += Time.deltaTime;
        ShowFrame();
    }

    private void ShowFrame()
    {
        if (frames == null || frames.Length == 0 || sprite == null) return;

        int index = (int)(frameClock * framesPerSecond);
        sprite.sprite = frames[loopFrames ? index % frames.Length : Mathf.Min(index, frames.Length - 1)];
    }
}
