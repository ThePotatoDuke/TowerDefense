using UnityEngine;
using DG.Tweening;

public class PlayerInteractionController : MonoBehaviour
{
    [SerializeField] private Player player;
    [SerializeField] private WeaponBase currentWeapon;


    [Header("Knockback Settings")]
    [SerializeField] private float knockbackDistance = 1f;
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private float bounceHeight = 1f;

    private Sequence currentKnockbackSeq;

    private void OnTriggerStay(Collider other)
    {

        // --- Enemy knockback/damage (existing code) ---
        if (other.TryGetComponent<IEnemy>(out var enemy) && !player.IsDead())
        {
            if (!player.IsInvulnerable)
            {
                Vector3 knockDir = (player.transform.position - other.transform.position).normalized;
                knockDir.y = 0;
                ApplyKnockback(knockDir);
                player.TakeDamage(enemy.ContactDamage);
            }
        }

        // Check if collider has a pickup component
        if (other.TryGetComponent<WeaponPickup>(out var pickup))
        {
            player.EquipWeapon(pickup.weaponSO.weaponPrefab);

            // Optionally destroy the pickup in the world
            Destroy(pickup.gameObject);
        }
    }

    private void ApplyKnockback(Vector3 direction)
    {
        // Kill any existing knockback sequence safely
        if (currentKnockbackSeq != null && currentKnockbackSeq.IsActive())
            currentKnockbackSeq.Kill(true);

        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        if (playerRb != null)
            playerRb.isKinematic = true;

        Vector3 startPos = player.transform.position;
        Vector3 intendedTarget = startPos + direction.normalized * knockbackDistance;

        // Prevent clipping into walls
        Vector3 boxHalfExtents = player.GetComponent<Collider>().bounds.extents;
        if (Physics.BoxCast(startPos, boxHalfExtents, direction.normalized, out RaycastHit hit, Quaternion.identity, knockbackDistance))
            intendedTarget = hit.point - direction.normalized * 0.05f;

        currentKnockbackSeq = DOTween.Sequence();

        // Horizontal movement
        currentKnockbackSeq.Append(player.transform.DOMove(intendedTarget, knockbackDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true));

        // Vertical bounce (up)
        currentKnockbackSeq.Join(player.transform.DOMoveY(intendedTarget.y + bounceHeight, knockbackDuration / 2f)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true));

        // Vertical bounce (down)
        currentKnockbackSeq.Join(player.transform.DOMoveY(intendedTarget.y, knockbackDuration / 2f)
            .SetEase(Ease.InQuad)
            .SetDelay(knockbackDuration / 2f)
            .SetUpdate(true));

        currentKnockbackSeq.OnComplete(() =>
        {
            if (playerRb != null)
                playerRb.isKinematic = false;

            // Reset y position to ground
            Vector3 finalPos = player.transform.position;
            player.transform.position = new Vector3(finalPos.x, startPos.y, finalPos.z);

            currentKnockbackSeq = null;
        });
    }
}
