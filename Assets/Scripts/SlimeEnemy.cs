using System.Collections;
using UnityEngine;
using DG.Tweening;

public class SlimeEnemy : EnemyBase
{
    [SerializeField] private SlimeDataSO data;
    [SerializeField] private Transform slimeVisual; // assign the child sprite here

    private Vector3 startPosition;

    public override EnemyDataSO Data => data;

    private void Start()
    {
        startPosition = transform.position;
        StartCoroutine(BounceRoutine());
    }


    private IEnumerator BounceRoutine()
    {
        Vector3 originalScale = slimeVisual.localScale; // scale the sprite, not parent
        Vector3 startPos = transform.position;

        while (true)
        {
            float upHeight = data.bounceHeight;
            float upDuration = data.bounceDuration;
            float downDuration = data.bounceDuration;
            float interval = data.bounceInterval;
            float moveDistance = data.moveDistance;

            transform.DOKill();

            Sequence bounceSeq = DOTween.Sequence();

            Vector3 midPos = startPos + new Vector3(moveDistance / 2f, upHeight, 0);
            Vector3 endPos = startPos + new Vector3(moveDistance, 0, 0);


            // Move UP + horizontal linear

            bounceSeq.Append(transform.DOMoveY(midPos.y, upDuration).SetEase(Ease.OutQuad));
            bounceSeq.Join(transform.DOMoveX(midPos.x, upDuration).SetEase(Ease.Linear));

            // Stretch at takeoff (sprite only)
            float takeoffStretchY = 1.4f;
            float takeoffStretchX = 1f / takeoffStretchY;
            bounceSeq.Join(slimeVisual.DOScale(
                new Vector3(originalScale.x * takeoffStretchX, originalScale.y * takeoffStretchY, originalScale.z),
                upDuration / 3f
            ));

            // Relax mid-air
            bounceSeq.Join(slimeVisual.DOScale(originalScale, upDuration / 3f).SetDelay(upDuration / 3f));


            // Move DOWN + horizontal linear + fall stretch
            // -------------------------------
            float fallStretchY = 1.2f;
            float fallStretchX = 1f / fallStretchY;

            bounceSeq.Append(transform.DOMoveY(endPos.y, downDuration).SetEase(Ease.InQuad));
            bounceSeq.Join(transform.DOMoveX(endPos.x, downDuration).SetEase(Ease.Linear));
            bounceSeq.Join(slimeVisual.DOScale(
                new Vector3(originalScale.x * fallStretchX, originalScale.y * fallStretchY, originalScale.z),
                downDuration / 1.5f
            ));

            // -------------------------------
            // Squash on landing
            // -------------------------------
            float squashY = 0.6f;
            float squashX = 1f / squashY;

            bounceSeq.Append(slimeVisual.DOScale(
                new Vector3(originalScale.x * squashX, originalScale.y * squashY, originalScale.z),
                upDuration / 3f
            ));

            // Bounce back to normal
            bounceSeq.Append(slimeVisual.DOScale(originalScale, upDuration / 3f));

            // -------------------------------
            // Small restore and interval
            // -------------------------------
            bounceSeq.Append(slimeVisual.DOScale(originalScale, 0.05f));
            bounceSeq.AppendInterval(interval);

            // Wait for sequence to finish
            yield return bounceSeq.WaitForCompletion();

            // Update startPos for next bounce
            startPos = endPos;
        }
    }
}
