using DG.Tweening;
using UnityEngine;

// A spike shot out by a bursting spike ball. Waits faintly in place while the burst is being warned,
// then flies straight over the ground pointing the way it's going, hurts the player if it reaches them,
// and fades out at the end of its range.
public class SpikeProjectile : MonoBehaviour
{
    private const float HitRadius = 0.35f;
    private const float FadeTime = 0.15f;
    private const float WaitingAlpha = 0.5f;

    private SpriteRenderer sprite;
    private Vector3 direction;
    private float speed;
    private float range;
    private float damage;
    private float travelled;
    private bool finished;
    private bool launched;
    private TrailRenderer trail;

    // A spike aimed and waiting, drawn faint, until Launch. Lets a warning show the real spikes.
    public static SpikeProjectile Create(Vector3 origin, Vector3 direction, Sprite spikeSprite, Material material,
                                         float speed, float range, float damage)
    {
        var spikeObject = new GameObject("Spike");
        spikeObject.transform.position = origin;

        var spike = spikeObject.AddComponent<SpikeProjectile>();
        spike.direction = direction.normalized;
        spike.speed = speed;
        spike.range = range;
        spike.damage = damage;

        spike.sprite = spikeObject.AddComponent<SpriteRenderer>();
        spike.sprite.sprite = spikeSprite;
        spike.sprite.sharedMaterial = material;
        spike.sprite.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        spike.sprite.receiveShadows = false;
        spike.sprite.color = new Color(1f, 1f, 1f, WaitingAlpha);
        spike.FaceTravelDirection();
        return spike;
    }

    public void Launch()
    {
        launched = true;
        sprite.color = Color.white;

        // White pixel trail, like the bats'
        FXSettings settings = FXSettings.Instance;
        if (settings != null)
            trail = PixelTrails.Attach(transform, Vector3.zero, settings.BatTrailMaterial, Color.white,
                                       settings.spikeTrailTime, settings.spikeTrailWidth, settings.spikeTrailAlpha);
    }

    private void Update()
    {
        if (!launched || finished) return;

        float step = speed * Time.deltaTime;
        transform.position += direction * step;
        travelled += step;

        Player player = Player.Instance;
        if (player != null && !player.IsDead())
        {
            Vector3 offset = player.transform.position - transform.position;
            offset.y = 0f;
            if (offset.sqrMagnitude <= HitRadius * HitRadius)
            {
                // Like touching an enemy: knocked back along the spike's flight, unless still invulnerable
                if (!player.IsInvulnerable)
                {
                    PlayerInteractionController interaction = player.GetComponentInChildren<PlayerInteractionController>();
                    if (interaction != null)
                        interaction.Knockback(direction);
                    player.TakeDamage(damage);
                }
                Finish();
                return;
            }
        }

        if (travelled >= range)
            Finish();
    }

    private void LateUpdate()
    {
        FaceTravelDirection();
    }

    // Face the camera like every sprite, turned so the tip (the art points up) leads on screen
    private void FaceTravelDirection()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        Transform view = cam.transform;
        float screenX = Vector3.Dot(direction, view.right);
        float screenY = Vector3.Dot(direction, view.up);
        float angle = Mathf.Atan2(-screenX, screenY) * Mathf.Rad2Deg;
        transform.rotation = view.rotation * Quaternion.Euler(0f, 0f, angle);
    }

    private void Finish()
    {
        finished = true;
        PixelTrails.Release(trail); // fades out on its own
        trail = null;
        sprite.DOFade(0f, FadeTime).SetLink(gameObject).OnComplete(() => Destroy(gameObject));
    }
}
