using UnityEngine;
using DG.Tweening;

public class PlayerInteractionController : MonoBehaviour
{
    [SerializeField] private Player player;


    [Header("Knockback Settings")]
    [SerializeField] private float knockbackDistance = 1f;
    [SerializeField] private float knockbackDuration = 0.2f;
    [SerializeField] private float bounceHeight = 1f;

    private Sequence currentKnockbackSeq;

    private void OnTriggerStay(Collider other)
    {
        IEnemy enemy = other.GetComponentInParent<IEnemy>();
        if (enemy != null && !player.IsDead() && GameStateManager.CurrentState == GameState.Playing)
        {
            if (!player.IsInvulnerable)
            {
                Vector3 knockDir = (player.transform.position - other.transform.position).normalized;
                knockDir.y = 0;
                Knockback(knockDir);
                player.TakeDamage(enemy.ContactDamage);
            }
        }

        if (other.TryGetComponent<WeaponPickup>(out var pickup))
        {
            player.EquipWeapon(pickup.weaponSO.weaponPrefab);
            Destroy(pickup.gameObject);
        }
    }


    // Push the player away in this direction with a little hop (enemy contact, enemy spikes)
    public void Knockback(Vector3 direction)
    {
        // Kill any existing knockback sequence safely
        if (currentKnockbackSeq != null && currentKnockbackSeq.IsActive())
            currentKnockbackSeq.Kill(true);

        // The knockback moves the transform directly, which interpolation would fight, so it's off meanwhile
        Rigidbody playerRb = player.GetComponent<Rigidbody>();
        RigidbodyInterpolation interpolation = RigidbodyInterpolation.Interpolate;
        if (playerRb != null)
        {
            if (playerRb.interpolation != RigidbodyInterpolation.None)
                interpolation = playerRb.interpolation;
            playerRb.isKinematic = true;
            playerRb.interpolation = RigidbodyInterpolation.None;
        }

        Vector3 startPos = player.transform.position;
        Vector3 intendedTarget = startPos + direction.normalized * knockbackDistance;

        // Prevent clipping into walls
        Vector3 boxHalfExtents = player.GetComponent<Collider>().bounds.extents;
        if (Physics.BoxCast(startPos, boxHalfExtents, direction.normalized, out RaycastHit hit, Quaternion.identity, knockbackDistance,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
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
            // Reset y position to ground
            Vector3 finalPos = player.transform.position;
            player.transform.position = new Vector3(finalPos.x, startPos.y, finalPos.z);

            if (playerRb != null)
            {
                playerRb.position = player.transform.position;
                playerRb.isKinematic = false;
                playerRb.interpolation = interpolation;
            }

            currentKnockbackSeq = null;
        });
    }
}
