using DG.Tweening;
using UnityEngine;

// One of a group of Checker Slimes (see CheckerSlimeDataSO). It doesn't move on its own: its group
// (CheckerSlimeGroup) tells it when and where to jump. Each jump is an arc along the path with the slimes'
// squash and stretch; the body collider rises with it, so it's hit where it's seen, in the air too. A group
// starts lined up behind the start of the path (progress below 0: back along the way the path comes in, in
// the forest) and leapfrogs onto it, already facing the way the path goes.
public class CheckerSlimeEnemy : EnemyBase
{
    [Header("Data")]
    [SerializeField] private CheckerSlimeDataSO data;

    [Header("Helpers")]
    [Tooltip("The child with the body collider, holding the hopping sprite.")]
    [SerializeField] private Transform slimeChild;

    private Vector3 originalScale;
    private Transform hopRoot;
    private float hopRestY;
    private SphereCollider bodyCollider;
    private Vector3 bodyRestCenter;
    private Sequence jump;

    public override EnemyDataSO Data => data;
    public override bool HealthBarFollowsSprite => true;
    // Lined up behind the path's start (off screen, in the forest) it can't be hurt, so a group isn't wiped
    // out by weapons, weeds or blasts near the start before it's even been seen
    public override bool CanBeHit => distancePercentage >= 0f;
    public CheckerSlimeDataSO CheckerData => data;
    public bool InAir => jump != null && jump.IsActive();
    // No vaulting a roller mid-leapfrog
    public override bool CanDodge => !InAir && base.CanDodge;
    // Where it is on the path, or where it'll land if it's jumping (0 to 1)
    public float LandingProgress { get; private set; }

    protected override void Awake()
    {
        base.Awake();
        originalScale = visual.localScale;

        // Jumps move the child of slimeChild that holds the sprite, so they never fight a roller vault,
        // which lifts slimeChild itself
        hopRoot = visual;
        while (hopRoot.parent != null && hopRoot.parent != slimeChild && hopRoot.parent != transform)
            hopRoot = hopRoot.parent;
        hopRestY = hopRoot.localPosition.y;

        if (slimeChild != null && slimeChild.TryGetComponent(out bodyCollider))
            bodyRestCenter = bodyCollider.center;
    }

    // Black or white, by the animations it plays
    public void SetWhite(bool white)
    {
        var animator = GetComponentInChildren<Animator>();
        RuntimeAnimatorController controller = white ? data.whiteAnimator : data.blackAnimator;
        if (animator != null && controller != null)
            animator.runtimeAnimatorController = controller;
    }

    // Its place in the line: on the path (0 to 1) or behind its start (below 0)
    public void PlaceInLine(float progress)
    {
        PlaceOnPath(Mathf.Max(0f, progress));
        distancePercentage = LandingProgress = Mathf.Min(progress, 1f);
        transform.position = PathPosition(distancePercentage);
        FaceAlongPath();
    }

    // The group moves it
    public override void Move() { }

    protected override void Update()
    {
        base.Update();
        if (isDead) return;

        // Behind the start of the path, it's out on the line the path comes in along
        if (distancePercentage < 0f)
            transform.position = PathPosition(distancePercentage);

        // A roller vault moves it along the path; it lands wherever that leaves it
        if (IsDodging)
            LandingProgress = distancePercentage;
    }

    // On the path, or for progress below 0 that far back from its start, straight out the way it comes in
    private Vector3 PathPosition(float progress)
    {
        if (splineContainer == null) return transform.position;
        if (progress >= 0f) return splineContainer.EvaluatePosition(progress);

        Vector3 start = splineContainer.EvaluatePosition(0f);
        Vector3 along = splineContainer.EvaluateTangent(0f);
        along.y = 0f;
        return along.sqrMagnitude > 0.0001f ? start + along.normalized * (progress * splineLength) : start;
    }


    protected override void LateUpdate()
    {
        base.LateUpdate();
        if (bodyCollider == null || hopRoot == null) return;
        // hopRoot is a child of slimeChild, so its offset is already in the collider's space
        bodyCollider.center = bodyRestCenter + Vector3.up * (hopRoot.localPosition.y - hopRestY);
    }

    // An arc to this spot on the path (0 to 1), this high and long. It goes round the arc at an even pace,
    // like a point on a wheel's rim (slower sideways at takeoff and landing), so an arc half as high as it
    // is long is a perfect half circle.
    public void JumpTo(float progress, float height, float duration)
    {
        if (isDead) return;
        float from = distancePercentage;
        LandingProgress = Mathf.Min(progress, 1f);
        float half = duration * 0.5f;

        jump?.Kill();
        visual.DOKill();
        hopRoot.DOKill();
        jump = DOTween.Sequence().SetLink(gameObject)
            .Append(DOVirtual.Float(0f, Mathf.PI, duration, angle =>
            {
                distancePercentage = Mathf.Lerp(from, LandingProgress, (1f - Mathf.Cos(angle)) * 0.5f);
                Vector3 position = hopRoot.localPosition;
                hopRoot.localPosition = new Vector3(position.x, hopRestY + Mathf.Sin(angle) * height, position.z);
            }).SetEase(Ease.Linear))
            .Join(visual.DOScale(Stretched(data.takeoffStretch), half / 1.5f))
            .Insert(half / 1.5f, visual.DOScale(originalScale, half / 3f))
            .Insert(half, visual.DOScale(Stretched(data.fallStretch), half / 1.5f))
            // Squash on landing, then springs back with a little wobble
            .Append(visual.DOScale(Stretched(data.squash), data.squashTime).SetEase(Ease.OutQuad))
            .Append(visual.DOScale(originalScale, data.springBackTime).SetEase(Ease.OutBack));
    }

    private Vector3 Stretched(float y) =>
        new(originalScale.x / y, originalScale.y * y, originalScale.z);

    protected override void OnDisable()
    {
        base.OnDisable();
        jump?.Kill();
        if (visual != null)
            visual.DOKill();
    }

    protected override void Die()
    {
        jump?.Kill();
        base.Die();
    }
}
