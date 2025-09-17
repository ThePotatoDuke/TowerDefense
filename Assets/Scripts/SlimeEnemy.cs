using System.Collections;
using UnityEngine;
using DG.Tweening;
using UnityEditor;

public class SlimeEnemy : EnemyBase
{
    [SerializeField] private SlimeDataSO data;
    [SerializeField] private Transform slimeVisual; // child sprite/visual


    private Vector3 childOriginalLocalPos;
    private Vector3 originalScale;
    private float moveDistance;
    public override EnemyDataSO Data => data;


    private void Awake()
    {

        childOriginalLocalPos = slimeVisual.localPosition;
        originalScale = slimeVisual.localScale;
        moveDistance = data.moveDistance;
    }

    private void Start()
    {
        // StartCoroutine(BounceRoutine());
    }


    // Parent-only horizontal movement (X). Safe to call while child animates.
    public override void Move()
    {
        // float duration = 2f * data.bounceDuration;
        // // Move parent on X by moveDistance (relative to current position)
        // transform.DOMoveX(transform.position.x + moveDistance, duration)
        //          .SetEase(Ease.Linear)
        //          .SetLink(gameObject); // automatically kill if object destroyed
    }

    private IEnumerator BounceRoutine()
    {
        while (true)
        {
            float upHeight = data.bounceHeight;
            float upDuration = data.bounceDuration;
            float downDuration = data.bounceDuration;
            float interval = data.bounceInterval;

            // Kill old tweens
            slimeVisual.DOKill();
            transform.DOKill();

            // Move horizontally
            Move();

            Sequence bounceSeq = DOTween.Sequence();

            // --- Move UP (PARENT collider moves on Y) ---
            bounceSeq.Append(
                transform.DOMoveY(transform.position.y + upHeight, upDuration)
                         .SetEase(Ease.OutQuad)
            );

            // --- Takeoff stretch (child only) ---
            float takeoffStretchY = 1.4f;
            float takeoffStretchX = 1f / takeoffStretchY;
            bounceSeq.Join(
                slimeVisual.DOScale(
                    new Vector3(originalScale.x * takeoffStretchX, originalScale.y * takeoffStretchY, originalScale.z),
                    upDuration / 3f
                )
            );

            // Relax mid-air
            bounceSeq.Join(
                slimeVisual.DOScale(originalScale, upDuration / 3f).SetDelay(upDuration / 3f)
            );

            // --- Move DOWN (PARENT collider back to ground) ---
            bounceSeq.Append(
                transform.DOMoveY(transform.position.y, downDuration)
                         .SetEase(Ease.InQuad)
            );

            // Fall stretch
            float fallStretchY = 1.2f;
            float fallStretchX = 1f / fallStretchY;
            bounceSeq.Join(
                slimeVisual.DOScale(
                    new Vector3(originalScale.x * fallStretchX, originalScale.y * fallStretchY, originalScale.z),
                    downDuration / 1.5f
                )
            );

            // --- Squash on landing ---
            float squashY = 0.6f;
            float squashX = 1f / squashY;
            bounceSeq.Append(
                slimeVisual.DOScale(
                    new Vector3(originalScale.x * squashX, originalScale.y * squashY, originalScale.z),
                    upDuration / 3f
                )
            );

            // Return to normal scale
            bounceSeq.Append(
                slimeVisual.DOScale(originalScale, upDuration / 3f)
            );

            // Small restore + wait interval
            bounceSeq.Append(slimeVisual.DOScale(originalScale, 0.05f));
            bounceSeq.AppendInterval(interval);

            yield return bounceSeq.WaitForCompletion();

            // Ensure reset
            slimeVisual.localPosition = childOriginalLocalPos;
            slimeVisual.localScale = originalScale;
        }
    }


    private void OnDisable()
    {
        // clean up tweens if object gets disabled/destroyed
        slimeVisual.DOKill();
        transform.DOKill();
    }
}
