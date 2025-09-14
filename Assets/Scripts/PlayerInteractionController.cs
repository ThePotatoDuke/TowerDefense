using UnityEngine;
using DG.Tweening;
using System;

[RequireComponent(typeof(Player))]
public class PlayerInteractionController : MonoBehaviour
{
    private Player player;
    private Rigidbody playerRb;

    [Header("Knockback Settings")]
    [SerializeField] private float knockbackDistance = 10f;
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private float bounceHeight = .3f;


    private void Awake()
    {
        player = GetComponent<Player>();
        playerRb = GetComponent<Rigidbody>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.TryGetComponent<IEnemy>(out var enemy))
        {
            float damageAmount = enemy.ContactDamage;



            // Apply knockback with small bounce
            Vector3 knockDir = (transform.position - other.transform.position).normalized;
            knockDir.y = 0; //reset vertical difference

            ApplyKnockback(knockDir);
            // Apply damage
            player.TakeDamage(damageAmount);
        }
    }


    //Used dotween here cuz i want to keep my player rigidbody kinematic lol it is a lot snappier also
    private void ApplyKnockback(Vector3 direction)
    {
        if (player.IsInvulnerable)
        {
            Debug.Log(player.IsInvulnerable);
            return;
        }
        Vector3 startPos = transform.position;
        Vector3 targetPos = startPos + direction * knockbackDistance;

        Sequence knockbackSeq = DOTween.Sequence();

        // Horizontal movement
        knockbackSeq.Append(transform.DOMove(targetPos, knockbackDuration).SetEase(Ease.OutQuad));

        // Vertical bounce: go up for half duration (joined with horizontal)
        knockbackSeq.Join(transform.DOMoveY(targetPos.y + bounceHeight, knockbackDuration / 2f).SetEase(Ease.OutQuad));

        // Vertical comes back down after first half (joined with delay)
        knockbackSeq.Join(transform.DOMoveY(targetPos.y, knockbackDuration / 2f).SetEase(Ease.InQuad).SetDelay(knockbackDuration / 2));

    }
}
