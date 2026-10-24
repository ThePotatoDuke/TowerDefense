using System.Collections;
using UnityEngine;
using DG.Tweening;

public class SlimeEnemy : EnemyBase
{
    [Header("Data")]
    [SerializeField] private SlimeDataSO data;

    [Header("Visuals")]

    [Header("Helpers")]
    [SerializeField] private Transform slimeChild;

    private Vector3 originalScale;
    private enum HopType { ShortForward, LongForward, Backward }
    private HopType currentHopType;
    private HopType lastHop = HopType.ShortForward;

    private int shortHopCounter = 0;
    private int consecutiveBackForwardUnits = 0;

    public override EnemyDataSO Data => data;

    protected override void Awake()
    {
        base.Awake();
        if (visual == null)
            visual = transform.Find("SlimeVisual");

        originalScale = visual.localScale;

        // Hops move the child of slimeChild that holds the sprite, so they never fight a roller vault,
        // which lifts slimeChild itself
        hopRoot = visual;
        while (hopRoot.parent != null && hopRoot.parent != slimeChild && hopRoot.parent != transform)
            hopRoot = hopRoot.parent;
        hopRestY = hopRoot.localPosition.y;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        visual.DOKill();
    }

    private void Start()
    {
        StartCoroutine(BounceRoutine());
    }

    protected override void Die()
    {
        StopAllCoroutines(); // no new hops while dissolving
        base.Die();
    }
    private float currentHopDistance;   // distance for Move()
    private float currentUpDuration;     // duration for spline movement
    private Tween moveTween;
    private Sequence bounceSeq;
    private Transform hopRoot;
    private float hopRestY;

    protected override void Update()
    {
        base.Update();

        // A vault moves the slime itself; drop the current hop so the next one starts from where it lands
        if (IsDodging)
        {
            if (moveTween != null && moveTween.IsActive())
                moveTween.Kill();

            // Settle out of any hop in progress, so it doesn't hop in mid-air during the vault
            if (bounceSeq != null && bounceSeq.IsActive())
            {
                bounceSeq.Kill();
                visual.DOKill();
                hopRoot.DOLocalMoveY(hopRestY, 0.1f).SetLink(gameObject);
                visual.DOScale(originalScale, 0.1f).SetLink(gameObject);
            }
        }
    }

    public override void Move()
    {
        if (hasReachedEnd) return;

        float stepPercentage = currentHopDistance / splineLength;
        float targetPercentage = Mathf.Clamp01(distancePercentage + stepPercentage);

        moveTween = DOTween.To(() => distancePercentage, x => distancePercentage = x, targetPercentage, currentUpDuration * 2f)
               .SetEase(Ease.Linear)
               .SetLink(gameObject);
    }

    private IEnumerator BounceRoutine()
    {
        while (true)
        {
            // No new hop while vaulting over a roller
            while (IsDodging)
                yield return null;

            // Determine next hop distance
            currentHopDistance = GetNextHop();

            // Base bounce parameters
            float upHeight = data.bounceHeight;
            float upDuration = data.bounceDuration;
            float downDuration = data.bounceDuration;
            float interval = data.bounceInterval;

            // Apply multipliers for LongForward hops
            if (currentHopType == HopType.LongForward)
            {
                upHeight *= data.longHopHeightMultiplier;
                upDuration *= data.longHopDurationMultiplier;
                downDuration *= data.longHopDurationMultiplier;
            }

            // Store upDuration for spline movement in Move()
            currentUpDuration = upDuration;

            visual.DOKill();
            hopRoot.DOKill();

            // Move along spline
            Move();

            // --- Bounce animation sequence ---
            // Linked so the whole sequence dies with the slime (nested tweens can't be killed via DOKill)
            bounceSeq = DOTween.Sequence().SetLink(gameObject);

            // Bounce up, to a fixed height above rest so hops can never drift
            bounceSeq.Append(hopRoot.DOLocalMoveY(hopRestY + upHeight, upDuration)
                              .SetEase(Ease.OutQuad));

            // Takeoff stretch for visual squash/stretch effect
            float takeoffStretchY = data.takeoffStretch;
            float takeoffStretchX = 1f / takeoffStretchY;
            bounceSeq.Join(visual.DOScale(
                new Vector3(originalScale.x * takeoffStretchX, originalScale.y * takeoffStretchY, originalScale.z),
                upDuration / 3f));

            // Relax mid-air
            bounceSeq.Join(visual.DOScale(originalScale, upDuration / 3f)
                              .SetDelay(upDuration / 3f));

            // Move down
            bounceSeq.Append(hopRoot.DOLocalMoveY(hopRestY, downDuration)
                              .SetEase(Ease.InQuad));

            // Fall stretch for visual effect
            float fallStretchY = data.fallStretch;
            float fallStretchX = 1f / fallStretchY;
            bounceSeq.Join(visual.DOScale(
                new Vector3(originalScale.x * fallStretchX, originalScale.y * fallStretchY, originalScale.z),
                downDuration / 1.5f));

            // Squash on landing
            float squashY = data.squash;
            float squashX = 1f / squashY;
            bounceSeq.Append(visual.DOScale(
                new Vector3(originalScale.x * squashX, originalScale.y * squashY, originalScale.z),
                upDuration / 3f));

            // Return to normal scale
            bounceSeq.Append(visual.DOScale(originalScale, upDuration / 3f));

            // Wait interval before next hop
            bounceSeq.AppendInterval(interval);

            yield return bounceSeq.WaitForCompletion();

            // Special wait for ShortShortLong pattern
            if (data.hopPattern == SlimeDataSO.HopPattern.ShortShortLong &&
                lastHop == HopType.LongForward && shortHopCounter == 0)
            {
                yield return new WaitForSeconds(data.longHopWaitTime);
            }
        }
    }

    private float GetNextHop()
    {
        float shortHop = data.baseHopDistance;
        float longHop = data.baseHopDistance * data.longHopDistanceMultiplier;
        float backHop = -shortHop;

        switch (data.hopPattern)
        {
            case SlimeDataSO.HopPattern.Regular:
                currentHopType = HopType.ShortForward;
                break;

            case SlimeDataSO.HopPattern.ShortShortLong:
                if (shortHopCounter >= 2)
                {
                    currentHopType = HopType.LongForward;
                    shortHopCounter = 0;
                }
                else
                {
                    currentHopType = HopType.ShortForward;
                    shortHopCounter++;
                }
                break;

            case SlimeDataSO.HopPattern.BackAndForward:

                if (lastHop == HopType.Backward)
                {
                    // Forward hop after a backward hop
                    if (consecutiveBackForwardUnits >= data.maxBackForward)
                    {
                        currentHopType = HopType.LongForward; // force leap if too many back hops
                    }
                    else if (consecutiveBackForwardUnits < data.minBackForward)
                    {
                        currentHopType = HopType.ShortForward; // enforce minimum back hops
                    }
                    else
                    {
                        // Randomly choose short or long forward based on inspector field
                        currentHopType = (Random.value < data.longForwardChance)
                                         ? HopType.LongForward
                                         : HopType.ShortForward;
                    }

                    // Reset counter only on long forward hop
                    if (currentHopType == HopType.LongForward)
                        consecutiveBackForwardUnits = 0;
                }
                else
                {
                    // Decide whether to go backward
                    if (consecutiveBackForwardUnits < data.minBackForward || Random.value < data.backChance)
                    {
                        currentHopType = HopType.Backward;
                        consecutiveBackForwardUnits++; // count only backward hops
                    }
                    else
                    {
                        // Forward hop: choose short or long based on inspector field
                        currentHopType = (Random.value < data.longForwardChance)
                                         ? HopType.LongForward
                                         : HopType.ShortForward;

                        // Reset counter if long forward hop
                        if (currentHopType == HopType.LongForward)
                            consecutiveBackForwardUnits = 0;
                    }
                }
                break;


            default:
                currentHopType = HopType.ShortForward;
                break;
        }

        lastHop = currentHopType;

        return currentHopType switch
        {
            HopType.ShortForward => shortHop,
            HopType.LongForward => longHop,
            HopType.Backward => backHop,
            _ => shortHop
        };
    }
}
