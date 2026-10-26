using UnityEngine;

// A shot fired down at the ground by a flying enemy (Frills). A GroundMarker shows where it will land the
// moment it's fired; it flies there in a straight line, pointing the way it's going on screen, and hits
// the player (knocking them back) if they're still standing on the marker when it lands. It can't be
// stopped, only dodged.
public class EnemyBullet : MonoBehaviour
{
    private Vector3 start;
    private Vector3 target;
    private FrillsDataSO data;
    private SpriteRenderer sprite;
    private GroundMarker marker;
    private Material material;
    private float time;

    public static void Fire(Vector3 from, Vector3 target, FrillsDataSO data)
    {
        var bullet = new GameObject("EnemyBullet").AddComponent<EnemyBullet>();
        bullet.start = from;
        bullet.target = target;
        bullet.data = data;
        bullet.transform.position = from;
        bullet.marker = GroundMarker.Show(target);

        bullet.sprite = bullet.gameObject.AddComponent<SpriteRenderer>();
        bullet.sprite.sortingOrder = 10; // over the sprites it flies past
        FXSettings settings = FXSettings.Instance;
        if (settings != null && settings.spriteFXShader != null)
        {
            bullet.material = new Material(settings.spriteFXShader) { name = "EnemyBullet (Instance)" };
            bullet.sprite.sharedMaterial = bullet.material;
        }
        bullet.Animate();
        bullet.FaceTravel();
    }

    private void Update()
    {
        time += Time.deltaTime;
        float t = Mathf.Clamp01(time / Mathf.Max(0.01f, data.shotTravelTime));
        transform.position = Vector3.Lerp(start, target, t);
        Animate();
        FaceTravel();

        if (t >= 1f)
            Land();
    }

    private void Land()
    {
        // Like the spike balls' spikes: knocked away from where it lands (or on the way the shot was going,
        // if hit dead centre), and hurt, unless still flashing from the last hit
        Player player = Player.Instance;
        if (player != null && !player.IsDead() && !player.IsInvulnerable)
        {
            Vector3 offset = player.transform.position - target;
            offset.y = 0f;
            if (offset.magnitude <= data.shotHitRadius)
            {
                Vector3 away = offset.sqrMagnitude > 0.01f ? offset : target - start;
                away.y = 0f;
                PlayerInteractionController interaction = player.GetComponentInChildren<PlayerInteractionController>();
                if (interaction != null && away.sqrMagnitude > 0.0001f)
                    interaction.Knockback(away.normalized);
                player.TakeDamage(data.shotDamage);
            }
        }

        SparkBurst.Play(target + Vector3.up * 0.2f);
        if (marker != null)
            marker.Hide();
        Destroy(gameObject);
    }

    private void Animate()
    {
        if (data.shotFrames == null || data.shotFrames.Length == 0) return;
        sprite.sprite = data.shotFrames[(int)(time * data.shotFramesPerSecond) % data.shotFrames.Length];
    }

    // Faces the camera like the other sprites, turned so the art (which points right) points the way
    // the shot is heading on screen
    private void FaceTravel()
    {
        Camera view = Camera.main;
        if (view == null) return;

        Vector3 travel = target - start;
        float angle = Mathf.Atan2(Vector3.Dot(travel, view.transform.up), Vector3.Dot(travel, view.transform.right)) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.LookRotation(view.transform.forward, view.transform.up) * Quaternion.Euler(0f, 0f, angle);
    }

    private void OnDestroy()
    {
        if (material != null)
            Destroy(material);
    }
}
