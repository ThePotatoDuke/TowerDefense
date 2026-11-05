using System.Collections;
using DG.Tweening;
using UnityEngine;

// A boss wave's Spike Rain: every so often a spike ball falls out of the sky somewhere near the player
// (where they're heading, a little ahead), with a marker on the ground to warn where. Landing on the
// player hurts them; then it warns where its spikes will fly, like a spike ball bursting, fires them all
// round and pops. The balls are only for show (made from a spike ball enemy's prefab, stripped of
// everything but its look), so they aren't enemies: nothing can hit them and they don't hold up the wave.
public class SpikeRain : MonoBehaviour
{
    private const float MinDistance = 1f;  // never right on top of the player
    private const float Lead = 0.6f;       // seconds of the player's movement it aims ahead by
    private const float LandRadius = 0.7f; // a player within this of a ball on the ground is touching it

    private SpikeBallEnemy prefab;
    private SpikeBallDataSO data;
    private float interval;
    private float radius;
    private Vector3 lastPlayerPosition;
    private Vector3 playerVelocity;
    private bool stopped;

    public static SpikeRain Begin(SpikeBallEnemy prefab, float interval, float radius)
    {
        if (prefab == null || prefab.Data is not SpikeBallDataSO data) return null;
        var rain = new GameObject("SpikeRain").AddComponent<SpikeRain>();
        rain.prefab = prefab;
        rain.data = data;
        rain.interval = Mathf.Max(0.2f, interval);
        rain.radius = Mathf.Max(MinDistance, radius);
        rain.StartCoroutine(rain.Run());
        return rain;
    }

    // No more fall; the ones already falling finish
    public void Stop()
    {
        stopped = true;
        Destroy(gameObject, 5f);
    }

    private void Update()
    {
        Player player = Player.Instance;
        if (player == null || Time.deltaTime <= 0f) return;
        Vector3 position = player.transform.position;
        playerVelocity = Vector3.Lerp(playerVelocity, (position - lastPlayerPosition) / Time.deltaTime, 0.2f);
        lastPlayerPosition = position;
    }

    private IEnumerator Run()
    {
        while (true)
        {
            yield return new WaitForSeconds(interval * Random.Range(0.7f, 1.3f));
            if (stopped) yield break;
            Player player = Player.Instance;
            if (player == null || GameStateManager.CurrentState != GameState.Playing) continue;

            Vector2 offset = Random.insideUnitCircle * radius;
            if (offset.magnitude < MinDistance)
                offset = (offset.sqrMagnitude > 0.0001f ? offset.normalized : Vector2.right) * MinDistance;
            Vector3 aim = player.transform.position + Vector3.ProjectOnPlane(playerVelocity, Vector3.up) * Lead;
            Vector3 ground = new(aim.x + offset.x, player.transform.position.y, aim.z + offset.y);
            StartCoroutine(Drop(ground));
        }
    }

    private IEnumerator Drop(Vector3 ground)
    {
        FXSettings settings = FXSettings.Instance;
        float warning = settings != null ? settings.dropWarningTime : 1.2f;
        float fallTime = settings != null ? Mathf.Min(settings.dropFallTime, warning) : 0.45f;
        float height = settings != null ? settings.dropHeight : 12f;

        GroundMarker marker = GroundMarker.Show(ground);
        yield return new WaitForSeconds(warning - fallTime);

        Transform ball = CreateBall();
        Vector3 top = ground + Vector3.up * height;
        for (float time = 0f; time < fallTime; time += Time.deltaTime)
        {
            float t = time / fallTime;
            ball.position = Vector3.Lerp(top, ground, t * t); // speeding up as it falls
            yield return null;
        }
        ball.position = ground;
        if (marker != null)
            marker.Hide();
        CameraShake.Shake(ground, settings != null ? settings.dropLandShake : 0.2f, 0.25f);
        Sounds.PlayAt(Sound.SpikeDrop, ground);

        TouchPlayer(ground);
        yield return Burst(ball, ground);
    }

    // Like touching an enemy: a player on the ball is hurt and knocked away from it (unless still flashing
    // from a hit)
    private void TouchPlayer(Vector3 ground)
    {
        Player player = Player.Instance;
        if (player == null || player.IsDead() || player.IsInvulnerable) return;

        Vector3 away = player.transform.position - ground;
        away.y = 0f;
        if (away.sqrMagnitude > LandRadius * LandRadius) return;

        PlayerInteractionController interaction = player.GetComponentInChildren<PlayerInteractionController>();
        if (interaction != null)
            interaction.Knockback(away.sqrMagnitude > 0.0001f ? away.normalized : Vector3.forward);
        player.TakeDamage(data.contactDamage);
    }

    // The spike ball's look and nothing else: made switched off (so none of its scripts start), stripped
    // of its enemy script, physics and health bar, then switched on
    private Transform CreateBall()
    {
        var holder = new GameObject("SpikeRainBall");
        holder.SetActive(false);
        SpikeBallEnemy copy = Instantiate(prefab, holder.transform);
        GameObject ball = copy.gameObject;

        foreach (HealthBar bar in ball.GetComponentsInChildren<HealthBar>(true))
            DestroyImmediate(bar.gameObject);
        foreach (TibbitPerch perch in ball.GetComponentsInChildren<TibbitPerch>(true))
            DestroyImmediate(perch);
        DestroyImmediate(copy);
        foreach (Collider collider in ball.GetComponentsInChildren<Collider>(true))
            DestroyImmediate(collider);
        foreach (Rigidbody body in ball.GetComponentsInChildren<Rigidbody>(true))
            DestroyImmediate(body);

        // The outlined pixel-art look and the ground shadow a spike ball adds to itself when it starts (it's a 3D
        // model, drawn as pixel art)
        MeshRenderer model = ball.GetComponentInChildren<MeshRenderer>(true); // before the imposter adds its quad
        ball.GetOrAddComponent<PixelImposter>();
        ball.GetOrAddComponent<BlobShadow>().Source = model;

        ball.transform.SetParent(null, true);
        Destroy(holder);
        ball.SetActive(true);
        return ball.transform;
    }

    // Like a spike ball's death burst: dashed lines run out where the spikes will go while the ball swells
    // and blinks, then the spikes fly out from its middle, level with it, and the ball pops
    private IEnumerator Burst(Transform ball, Vector3 ground)
    {
        int count = Mathf.Max(1, data.deathSpikeCount);
        // The ball's middle, as a spike ball's own burst uses (its visual), before it swells
        Transform visual = ball.Find("SpikeBallVisual");
        Vector3 center = visual != null ? visual.position : ground + Vector3.up * data.ballRadius;
        float startAngle = Random.Range(0f, 360f);
        float spikeStart = data.ballRadius * 0.8f;
        var lines = new Transform[count];
        var lineMaterials = new Material[count];
        var directions = new Vector3[count];
        for (int i = 0; i < count; i++)
        {
            directions[i] = Quaternion.Euler(0f, startAngle + 360f * i / count, 0f) * Vector3.forward;
            lines[i] = GroundLine.Create(ground, directions[i], data.spikeRange, data.spikeWarningColor, out lineMaterials[i]);
        }

        Vector3 size = ball.localScale;
        ball.DOScale(size * 1.25f, data.spikeWarningTime).SetEase(Ease.InQuad).SetLink(ball.gameObject);
        // It blinks red like a spike ball does when hit (its 3D model's colour, which the pixel art shows)
        MeshRenderer model = ball.GetComponentInChildren<MeshRenderer>(true);
        Material modelMaterial = model != null ? model.material : null;
        Color modelColor = modelMaterial != null ? modelMaterial.color : Color.white;
        float nextFlash = 0f;
        for (float elapsed = 0f; elapsed < data.spikeWarningTime; elapsed += Time.deltaTime)
        {
            TouchPlayer(ground);
            if (modelMaterial != null && elapsed >= nextFlash)
            {
                modelMaterial.DOKill();
                modelMaterial.color = modelColor;
                modelMaterial.DOColor(Color.red, 0.1f).SetLink(ball.gameObject)
                    .OnComplete(() => modelMaterial.color = modelColor);
                nextFlash += 0.15f;
            }

            float blink = (int)(elapsed * 10f) % 2 == 0 ? 1f : 0.5f;
            foreach (Material lineMaterial in lineMaterials)
            {
                if (lineMaterial == null) continue;
                GroundLine.MarchDashes(lineMaterial, elapsed, 30f);
                lineMaterial.SetColor("_Color", data.spikeWarningColor * blink);
            }
            yield return null;
        }

        for (int i = 0; i < count; i++)
        {
            if (lines[i] != null)
                Destroy(lines[i].gameObject);
            if (lineMaterials[i] != null)
                Destroy(lineMaterials[i]);
            if (data.spikeSprite != null)
                SpikeProjectile.Create(center + directions[i] * spikeStart, directions[i], data.spikeSprite,
                                       data.spikeMaterial, data.spikeSpeed, data.spikeRange - spikeStart,
                                       data.spikeDamage).Launch();
        }

        ball.DOKill();
        if (modelMaterial != null)
            modelMaterial.DOKill();
        ball.DOScale(0f, 0.15f).SetEase(Ease.InBack).SetLink(ball.gameObject)
            .OnComplete(() =>
            {
                Destroy(ball.gameObject);
                if (modelMaterial != null)
                    Destroy(modelMaterial); // its own copy, made for the blinking
            });
    }
}
