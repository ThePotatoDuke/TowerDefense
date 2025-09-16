using UnityEngine;
using DG.Tweening;
using System;

[RequireComponent(typeof(Player))]
public class PlayerInteractionController : MonoBehaviour
{
    private Player player;
    private Rigidbody playerRb;

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackDistance = 1;
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private float bounceHeight = 1f;


    private void Awake()
    {
        player = GetComponent<Player>();
        playerRb = GetComponent<Rigidbody>();
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.TryGetComponent<IEnemy>(out var enemy))
        {
            if (!player.IsInvulnerable && !player.IsDead())
            {
                float damageAmount = enemy.ContactDamage;

                // Knockback
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
        Vector3 startPos = transform.position;
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

        // Horizontal movement
        knockbackSeq.Append(transform.DOMove(intendedTarget, knockbackDuration).SetEase(Ease.OutQuad));

        // Vertical bounce (up)
        knockbackSeq.Join(transform.DOMoveY(intendedTarget.y + bounceHeight, knockbackDuration / 2f).SetEase(Ease.OutQuad));

        // Vertical bounce (down)
        knockbackSeq.Join(transform.DOMoveY(intendedTarget.y, knockbackDuration / 2f)
            .SetEase(Ease.InQuad)
            .SetDelay(knockbackDuration / 2f));
    }

}
