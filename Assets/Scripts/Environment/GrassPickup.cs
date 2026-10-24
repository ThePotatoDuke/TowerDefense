using DG.Tweening;
using UnityEngine;

// A cut blade of grass: pops out of a mown tuft, flutters down like a leaf and waits on the ground.
// When the player comes close it flies to them and is collected as currency.
public class GrassPickup : MonoBehaviour
{
    [System.Serializable]
    public class Settings
    {
        [Tooltip("Blades within this distance of the player fly to them.")]
        public float magnetRadius = 1.6f;
        [Tooltip("Seconds after the cut before a blade can be collected, so it's seen falling first.")]
        public float collectDelay = 0.5f;
        public float gravity = 5f;
        [Tooltip("Top falling speed; low values make blades drift down like leaves.")]
        public float maxFallSpeed = 0.7f;
        [Tooltip("How far blades sway side to side while falling.")]
        public float flutterAmount = 0.6f;
        public float flutterSpeed = 5f;
        [Tooltip("Fastest spin, in degrees per second; each blade picks a random speed and direction.")]
        public float maxSpin = 270f;
        [Tooltip("Seconds a blade waits on the ground before fading away.")]
        public float lifetime = 8f;
    }

    // Center of a resting blade above the ground
    private const float RestHeight = 0.15f;
    private const float CollectDistance = 0.15f;
    private const float FadeDuration = 1f;

    private Settings settings;
    private SpriteRenderer sprite;
    private Vector3 velocity;
    private float groundY;
    private float angle;
    private float spin;
    private float flutterPhase;
    private float age;
    private float landedAt;
    private bool landed;
    private bool attracted;
    private bool fading;
    private float attractSpeed;

    public void Launch(Sprite blade, Material material, Settings settings, Vector3 velocity, float groundY)
    {
        this.settings = settings;
        this.velocity = velocity;
        this.groundY = groundY;

        sprite = gameObject.AddComponent<SpriteRenderer>();
        sprite.sprite = blade;
        sprite.sharedMaterial = material;
        sprite.flipX = Random.value < 0.5f;
        sprite.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        sprite.receiveShadows = false;

        angle = Random.Range(0f, 360f);
        spin = Random.Range(-settings.maxSpin, settings.maxSpin);
        flutterPhase = Random.Range(0f, Mathf.PI * 2f);
    }

    private void Update()
    {
        if (settings == null) return;

        float dt = Time.deltaTime;
        age += dt;

        Player player = Player.Instance;
        bool playerAlive = player != null && !player.IsDead();

        if (!attracted && !fading && playerAlive && age >= settings.collectDelay && IsWithinMagnet(player))
            attracted = true;

        if (attracted)
        {
            if (playerAlive)
                FlyTo(player, dt);
            else
                attracted = false;
            return;
        }

        if (!landed)
            Fall(dt);
        else if (!fading && Time.time - landedAt >= settings.lifetime)
            FadeAway();
    }

    private void LateUpdate()
    {
        // Face the camera like every other sprite, at this blade's own tilt
        Camera cam = Camera.main;
        if (cam != null)
            transform.rotation = cam.transform.rotation * Quaternion.Euler(0f, 0f, angle);
    }

    private bool IsWithinMagnet(Player player)
    {
        Vector3 offset = player.transform.position - transform.position;
        offset.y = 0f;
        return offset.sqrMagnitude <= settings.magnetRadius * settings.magnetRadius;
    }

    private void Fall(float dt)
    {
        // Light, draggy fall: sideways drift dies off and the drop speed is capped
        velocity.y = Mathf.Max(velocity.y - settings.gravity * dt, -settings.maxFallSpeed);
        float drag = Mathf.Exp(-2f * dt);
        velocity.x *= drag;
        velocity.z *= drag;

        // Sway side to side on the way down, like a leaf
        Vector3 flutter = Vector3.zero;
        Camera cam = Camera.main;
        if (velocity.y < 0f && cam != null)
            flutter = cam.transform.right * (Mathf.Cos(age * settings.flutterSpeed + flutterPhase) * settings.flutterAmount);

        transform.position += (velocity + flutter) * dt;
        angle += spin * dt;

        if (transform.position.y <= groundY + RestHeight)
        {
            Vector3 position = transform.position;
            position.y = groundY + RestHeight;
            transform.position = position;
            landed = true;
            landedAt = Time.time;
        }
    }

    private void FlyTo(Player player, float dt)
    {
        // Accelerate toward the player's middle, spinning faster as it goes
        Vector3 target = player.transform.position + Vector3.up * 0.5f;
        attractSpeed += 25f * dt;
        transform.position = Vector3.MoveTowards(transform.position, target, (2f + attractSpeed) * dt);
        angle += spin * 2f * dt;

        if ((transform.position - target).sqrMagnitude <= CollectDistance * CollectDistance)
        {
            Currency.AddGrass(1);
            Destroy(gameObject);
        }
    }

    private void FadeAway()
    {
        fading = true;
        sprite.DOFade(0f, FadeDuration)
              .SetLink(gameObject)
              .OnComplete(() => Destroy(gameObject));
    }
}
