using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

// Zuma-style chain enemy: lines up behind the nearest Konga of the same kind ahead of it. When one in
// the middle is destroyed, the ones behind rush forward to close the gap, braking just before they rejoin.
public class KongaEnemy : EnemyBase
{
    private static readonly List<KongaEnemy> activeKongas = new();

    [SerializeField] private KongaDataSO data;

    private float currentSpeed;
    private bool catchingUp;
    private Tween bumpTween;

    public override EnemyDataSO Data => data;

    protected override void OnEnable()
    {
        base.OnEnable();
        activeKongas.Add(this);
    }

    protected override void OnDisable()
    {
        base.OnDisable();
        activeKongas.Remove(this);
    }

    protected override void Update()
    {
        if (isDead) return;

        base.Update(); // follow the spline, castle damage
        Move();
    }

    public override void Move()
    {
        if (hasReachedEnd || splineContainer == null) return;

        float targetSpeed = data.moveSpeed;
        KongaEnemy leader = FindLeader(out float gap);

        if (leader == null)
        {
            catchingUp = false;
        }
        else
        {
            float extra = gap - data.spacing;
            if (!catchingUp && extra > data.catchUpThreshold)
                catchingUp = true;

            if (catchingUp)
            {
                // Rush, easing off over the last stretch so it settles in just behind the leader
                float cruise = Mathf.Max(leader.PathSpeed, 0f) + data.moveSpeed * 0.25f;
                targetSpeed = Mathf.Lerp(cruise, data.catchUpSpeed, Mathf.Clamp01(extra / data.brakeDistance));

                if (extra <= 0.05f)
                {
                    catchingUp = false;
                    Bump();
                }
            }
            else if (extra < 0f)
            {
                // Too close: hang back so the line keeps its spacing
                targetSpeed = data.moveSpeed * Mathf.Clamp01(gap / data.spacing);
            }
        }

        currentSpeed = Mathf.MoveTowards(currentSpeed, targetSpeed, data.acceleration * Time.deltaTime);
        distancePercentage = Mathf.Clamp01(distancePercentage + currentSpeed * Time.deltaTime / splineLength);
    }

    // Nearest living Konga of the same kind further along the path, within chain range
    private KongaEnemy FindLeader(out float gap)
    {
        KongaEnemy leader = null;
        gap = float.MaxValue;

        foreach (KongaEnemy other in activeKongas)
        {
            if (other == this || other.isDead || other.data != data) continue;

            float distance = (other.distancePercentage - distancePercentage) * splineLength;
            if (distance <= 0f || distance >= gap) continue;

            leader = other;
            gap = distance;
        }

        if (gap > data.maxChainGap)
        {
            gap = float.MaxValue;
            return null;
        }
        return leader;
    }

    // Little squash when it rejoins the line
    private void Bump()
    {
        if (visual == null) return;

        // Only this tween: SpriteFlipper tweens the same transform's rotation
        bumpTween?.Kill(true);
        bumpTween = visual.DOPunchScale(new Vector3(0.25f, -0.2f, 0f), 0.25f, 6, 0.5f).SetLink(gameObject);
    }
}
