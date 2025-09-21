using System.Collections;
using UnityEngine;
using DG.Tweening;

public class SlimeEnemy : EnemyBase
{
    [Header("Data")]
    [SerializeField] private SlimeDataSO data;

    [Header("Visuals")]
    [SerializeField] private Transform slimeVisual;   // child sprite only
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
        if (slimeVisual == null)
            slimeVisual = transform.Find("SlimeVisual");

        originalScale = slimeVisual.localScale;
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        slimeVisual.DOKill();
    }

    private void Start()
    {
        StartCoroutine(BounceRoutine());
    }
    private float currentHopDistance;   // distance for Move()
    private float currentUpDuration;     // duration for spline movement

    public override void Move()
    {
        if (hasReachedEnd) return;

        float stepPercentage = currentHopDistance / splineContainer.CalculateLength();
        float targetPercentage = Mathf.Clamp01(distancePercentage + stepPercentage);

        DOTween.To(() => distancePercentage, x => distancePercentage = x, targetPercentage, currentUpDuration * 2f)
               .SetEase(Ease.Linear);
    }

    private IEnumerator BounceRoutine()
    {
        while (true)
        {
            // Get next hop
            currentHopDistance = GetNextHop();

            // Base bounce parameters
            float upHeight = data.bounceHeight;
            float upDuration = data.bounceDuration;
            float downDuration = data.bounceDuration;
            float interval = data.bounceInterval;

            // Apply leap multipliers only for LongForward hops
            if (currentHopType == HopType.LongForward)
            {
                upHeight *= data.leapHeightMultiplier;
                upDuration *= data.leapDurationMultiplier;
                downDuration *= data.leapDurationMultiplier;
            }

            // Store upDuration for Move()
            currentUpDuration = upDuration;

            slimeVisual.DOKill();

            // Move along spline
            Move();

            // --- Bounce animation sequence ---
            Sequence bounceSeq = DOTween.Sequence();

            bounceSeq.Append(slimeChild.DOLocalMoveY(upHeight, upDuration).SetRelative(true).SetEase(Ease.OutQuad));

            // Takeoff stretch
            float takeoffStretchY = data.takeoffStretch;
            float takeoffStretchX = 1f / takeoffStretchY;
            bounceSeq.Join(slimeVisual.DOScale(
                new Vector3(originalScale.x * takeoffStretchX, originalScale.y * takeoffStretchY, originalScale.z),
                upDuration / 3f));

            // Relax mid-air
            bounceSeq.Join(slimeVisual.DOScale(originalScale, upDuration / 3f).SetDelay(upDuration / 3f));

            // Move down
            bounceSeq.Append(slimeChild.DOLocalMoveY(-upHeight, downDuration).SetRelative(true).SetEase(Ease.InQuad));

            // Fall stretch
            float fallStretchY = data.fallStretch;
            float fallStretchX = 1f / fallStretchY;
            bounceSeq.Join(slimeVisual.DOScale(
                new Vector3(originalScale.x * fallStretchX, originalScale.y * fallStretchY, originalScale.z),
                downDuration / 1.5f));

            // Squash on landing
            float squashY = data.squash;
            float squashX = 1f / squashY;
            bounceSeq.Append(slimeVisual.DOScale(
                new Vector3(originalScale.x * squashX, originalScale.y * squashY, originalScale.z),
                upDuration / 3f));

            // Return to normal
            bounceSeq.Append(slimeVisual.DOScale(originalScale, upDuration / 3f));

            // Wait interval
            bounceSeq.AppendInterval(interval);

            yield return bounceSeq.WaitForCompletion();

            // Handle long hop wait for ShortShortLong pattern
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
        float longHop = data.baseHopDistance * data.longHopMultiplier;
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
