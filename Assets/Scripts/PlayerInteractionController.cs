using UnityEngine;
using DG.Tweening;
using System;


public class PlayerInteractionController : MonoBehaviour
{
    [SerializeField] private Player player;


    [Header("Knockback Settings")]
    [SerializeField] private float knockbackDistance = 1;
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private float bounceHeight = 1f;


    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent<IEnemy>(out var enemy))
        {
            if (!player.IsInvulnerable && !player.IsDead())
            {
                float damageAmount = enemy.ContactDamage;

                Vector3 knockDir = (transform.position - other.transform.position).normalized;
                knockDir.y = 0;

                ApplyKnockback(knockDir);
                player.TakeDamage(damageAmount);
            }
        }
    }





    //Used dotween here cuz i want to keep my player rigidbody kinematic lol it is a lot snappier also
    private void ApplyKnockback(Vector3 direction)
    {
        // Ensure no other knockback sequence is running
        DOTween.Kill(player.transform);

        // Get the Rigidbody component
        Rigidbody playerRb = player.GetComponent<Rigidbody>();

        // 1. Temporarily make the Rigidbody kinematic to disable physics forces
        if (playerRb != null)
        {
            playerRb.isKinematic = true;
        }

        Vector3 startPos = player.transform.position;
        Vector3 intendedTarget = startPos + direction.normalized * knockbackDistance;

        // BoxCast parameters: match your player collider
        Vector3 boxHalfExtents = player.GetComponent<Collider>().bounds.extents;
        RaycastHit hit;

        if (Physics.BoxCast(startPos, boxHalfExtents, direction.normalized, out hit, Quaternion.identity, knockbackDistance))
        {
            // Stop slightly before the wall
            intendedTarget = hit.point - direction.normalized * 0.05f;
        }

        Sequence knockbackSeq = DOTween.Sequence();

        // 2. Animate the transform directly with DOTween
        // Horizontal movement
        knockbackSeq.Append(player.transform.DOMove(intendedTarget, knockbackDuration).SetEase(Ease.OutQuad));

        // Vertical bounce (up)
        knockbackSeq.Join(player.transform.DOMoveY(intendedTarget.y + bounceHeight, knockbackDuration / 2f).SetEase(Ease.OutQuad));

        // Vertical bounce (down)
        knockbackSeq.Join(player.transform.DOMoveY(intendedTarget.y, knockbackDuration / 2f)
            .SetEase(Ease.InQuad)
            .SetDelay(knockbackDuration / 2f));

        // 3. On completion, re-enable the Rigidbody's physics and reset position
        knockbackSeq.OnComplete(() =>
        {
            // Re-enable physics simulation
            if (playerRb != null)
            {
                playerRb.isKinematic = false;
            }

            // Explicitly set the y position to the "ground" level to prevent falling
            Vector3 finalPos = player.transform.position;
            player.transform.position = new Vector3(finalPos.x, startPos.y, finalPos.z);
        });
    }

}
