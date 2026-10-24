using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

// A bomb carried along the path by a few little bomberrs; the whole group is one target.
// Defeat it, or let it reach the castle, and they throw the bomb up: it turns once around a random
// axis and lands nearby while the carriers run off and fade. On the ground the fuse burns and the bomb
// pulses faster and faster, then it explodes, hurting every enemy, the player and the castle in range.
public class BomberrEnemy : EnemyBase
{
    private static readonly int EmissionColorId = Shader.PropertyToID("_EmissionColor");
    private static readonly int SweepId = Shader.PropertyToID("_Sweep");
    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    private const float WarningOpenTime = 0.35f;

    [SerializeField] private BomberrDataSO data;
    [Tooltip("Child holding the collider and the bomb. The carriers are built under it.")]
    [SerializeField] private Transform body;
    [Tooltip("The bomb's pivot, at its middle so it turns in place when thrown. Holds the model.")]
    [SerializeField] private Transform bomb;
    [Tooltip("Where the fuse sparks come from. Move it in the prefab to line it up with the fuse tip.")]
    [SerializeField] private Transform fusePoint;

    private Renderer[] bombRenderers;
    private readonly List<Material> bombMaterials = new();
    private float bombBottom; // distance from the bomb's pivot down to its lowest point
    private readonly List<BomberrCarrier> carriers = new();
    private Transform warningRing;
    private Material warningMaterial;

    public override EnemyDataSO Data => data;

    // At the castle it throws its bomb instead of hitting the castle directly
    protected override bool DamagesCastle => false;

    protected override void Awake()
    {
        if (body == null)
            body = transform;

        bombRenderers = bomb != null ? bomb.GetComponentsInChildren<Renderer>() : new Renderer[0];
        if (bombRenderers.Length == 0)
            bomb = null;
        else
            bombBottom = bomb.position.y - GetBombBounds().min.y;

        base.Awake();

        if (bomb != null)
        {
            // Draw the 3D bomb as outlined pixel art, with a ground shadow like the sprites
            gameObject.GetOrAddComponent<PixelImposter>();
            gameObject.GetOrAddComponent<BlobShadow>().Source = bombRenderers[0];

            // Per-instance materials for the fuse glow (the same instances the hit flash uses)
            foreach (Renderer bombRenderer in bombRenderers)
            {
                Material material = bombRenderer.material;
                material.EnableKeyword("_EMISSION");
                bombMaterials.Add(material);
            }
        }

        // After base.Awake, so EnemyBase doesn't take a carrier's sprite as the enemy's visual
        BuildCarriers();
    }

    protected override void Update()
    {
        if (isDead) return;

        base.Update(); // follow the spline

        // At the castle: throw the bomb at it
        if (hasReachedEnd)
        {
            Die();
            return;
        }

        Move();
    }

    public override void Move()
    {
        if (hasReachedEnd || splineContainer == null) return;

        float step = data.moveSpeed * Time.deltaTime / splineLength;
        distancePercentage = Mathf.Clamp01(distancePercentage + step);
    }

    // Hop over rollers as a group: lift the body that holds the bomb, the carriers and the collider
    protected override Transform GetJumpRoot() => body != transform ? body : base.GetJumpRoot();

    public override void Flash(float duration = 0.1f)
    {
        base.Flash(duration); // the bomb
        foreach (BomberrCarrier carrier in carriers)
        {
            if (carrier != null)
                carrier.Flash();
        }
    }

    protected override float PlayDeathEffect()
    {
        StartCoroutine(Detonate());
        return -1f; // destroyed after the explosion
    }

    // --- Building the group ---

    private void BuildCarriers()
    {
        if (data.carryFrames == null || data.carryFrames.Length == 0) return;

        // The ring of carriers sits a little toward the camera, so the bomb doesn't hide them
        Vector3 towardCamera = Vector3.back;
        if (Camera.main != null)
        {
            towardCamera = -Camera.main.transform.forward;
            towardCamera.y = 0f;
            towardCamera = towardCamera.sqrMagnitude > 0.0001f ? towardCamera.normalized : Vector3.back;
        }
        Vector3 ringCenter = body.InverseTransformDirection(towardCamera) * data.carrierShiftTowardCamera;

        for (int i = 0; i < data.carrierCount; i++)
        {
            // Evenly around the bomb, offset so none hides exactly behind another
            float angle = (i + 0.5f) / data.carrierCount * Mathf.PI * 2f;

            var carrierObject = new GameObject($"Carrier{i}");
            carrierObject.transform.SetParent(body, false);
            carrierObject.transform.localPosition = ringCenter + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * data.carrierSpacing;

            var carrier = carrierObject.AddComponent<BomberrCarrier>();
            carrier.Setup(data.carryFrames, data.carryFramesPerSecond, data.spriteMaterial);
            carriers.Add(carrier);
        }
    }

    private Bounds GetBombBounds()
    {
        Bounds bounds = bombRenderers[0].bounds;
        for (int i = 1; i < bombRenderers.Length; i++)
            bounds.Encapsulate(bombRenderers[i].bounds);
        return bounds;
    }

    // --- Throw, fuse and explosion ---

    private IEnumerator Detonate()
    {
        ScatterCarriers();

        if (bomb == null)
        {
            Destroy(gameObject);
            yield break;
        }

        body.DOKill(); // stop any hop over a roller, so the bomb is thrown from where it is
        yield return ThrowBomb();
        yield return BurnFuse();

        Explode();
        Destroy(gameObject);
    }

    // The carriers let go and run off, away from the bomb, fading as they go
    private void ScatterCarriers()
    {
        Vector3 center = body.position;
        foreach (BomberrCarrier carrier in carriers)
        {
            if (carrier == null) continue;

            Vector3 direction = carrier.transform.position - center;
            direction.y = 0f;
            if (direction.sqrMagnitude < 0.0001f)
                direction = Random.insideUnitSphere;
            direction.y = 0f;
            direction = Quaternion.Euler(0f, Random.Range(-30f, 30f), 0f) * direction.normalized;

            carrier.Flee(data.runFrames, data.runFramesPerSecond, direction,
                         data.fleeSpeed * Random.Range(0.8f, 1.2f), data.fleeDuration);
        }
        carriers.Clear();
    }

    // Up and over to a spot nearby, one full turn around a random axis, then a small bounce
    private IEnumerator ThrowBomb()
    {
        Vector2 offset = Random.insideUnitCircle * data.throwDistance;
        Vector3 landing = new Vector3(
            bomb.position.x + offset.x,
            transform.position.y + bombBottom, // the root sits on the ground
            bomb.position.z + offset.y);

        Vector3 axis = Random.onUnitSphere;
        Quaternion startRotation = bomb.rotation;

        bomb.DOJump(landing, data.throwHeight, 1, data.throwDuration).SetEase(Ease.Linear).SetLink(gameObject);
        DOVirtual.Float(0f, 360f, data.throwDuration, angle => bomb.rotation = Quaternion.AngleAxis(angle, axis) * startRotation)
                 .SetEase(Ease.InOutSine)
                 .SetLink(gameObject);
        yield return new WaitForSeconds(data.throwDuration);

        bomb.rotation = startRotation;
        bomb.DOJump(landing, data.throwHeight * 0.1f, 1, 0.25f).SetLink(gameObject);
        if (GroundDecorator.Instance != null)
            GroundDecorator.Instance.CutGrassInRadius(landing, bombBottom + 0.2f);
        yield return new WaitForSeconds(0.25f);
    }

    // The fuse sparks while the bomb pulses red, faster and faster, swelling a little on each pulse.
    // A pixel ring on the ground marks how far the blast will reach, with a ring sweeping out on each beat.
    private IEnumerator BurnFuse()
    {
        ParticleSystem sparks = CreateFuseSparks();
        Vector3 restScale = bomb.localScale;
        float elapsed = 0f;
        float phase = 0f;

        Vector3 groundPoint = new Vector3(bomb.position.x, transform.position.y, bomb.position.z);
        float blastDiameter = data.explosionRadius * 2f;
        warningRing = ExplosionFX.CreateGroundRing(groundPoint, data.explosionRadius, data.warningColor, out warningMaterial);

        while (elapsed < data.fuseTime)
        {
            float t = elapsed / data.fuseTime;
            float interval = Mathf.Lerp(data.startPulseInterval, data.endPulseInterval, t * t);
            phase += Time.deltaTime / Mathf.Max(0.01f, interval);

            // Sharp peaks: bright on the beat, dark in between
            float glow = Mathf.Pow(0.5f + 0.5f * Mathf.Cos(phase * Mathf.PI * 2f), 3f);
            SetGlow(glow);
            bomb.localScale = restScale * (1f + 0.12f * glow);

            if (warningRing != null)
            {
                // Opens out from the bomb, then blinks with it while a ring sweeps outward on every beat
                float open = 1f - Mathf.Pow(1f - Mathf.Clamp01(elapsed / WarningOpenTime), 3f);
                ExplosionFX.SetSize(warningRing, warningMaterial, blastDiameter * open);
                warningMaterial.SetFloat(SweepId, Mathf.Repeat(phase, 1f));
                warningMaterial.SetFloat(AlphaId, 0.4f + 0.6f * glow);
            }

            elapsed += Time.deltaTime;
            yield return null;
        }

        if (sparks != null)
            sparks.Stop();
        RemoveWarningRing();
    }

    private void RemoveWarningRing()
    {
        if (warningRing != null)
            Destroy(warningRing.gameObject);
        if (warningMaterial != null)
            Destroy(warningMaterial);
        warningRing = null;
        warningMaterial = null;
    }

    private void OnDestroy()
    {
        RemoveWarningRing();
    }

    private void SetGlow(float amount)
    {
        Color emission = data.glowColor * amount;
        foreach (Material material in bombMaterials)
            material.SetColor(EmissionColorId, emission);
    }

    private void Explode()
    {
        Vector3 center = bomb.position;
        float radius = data.explosionRadius;

        ExplosionFX.Play(center, radius, transform.position.y);
        if (GroundDecorator.Instance != null)
            GroundDecorator.Instance.CutGrassInRadius(center, radius);

        // Every enemy in range, friend or foe of the bomberrs (a copy: some die and leave the list)
        if (EnemyManager.Instance != null)
        {
            foreach (IEnemy candidate in EnemyManager.Instance.GetAllEnemies())
            {
                if (candidate is EnemyBase enemy && enemy != this && !enemy.IsDead
                    && FlatDistance(enemy.transform.position, center) <= radius)
                    enemy.TakeDamage(data.enemyDamage);
            }
        }

        Player player = Player.Instance;
        if (player != null && !player.IsDead() && FlatDistance(player.transform.position, center) <= radius)
            player.TakeDamage(data.playerDamage);

        // The castle is big: measure to the nearest edge of its colliders, not its middle
        foreach (CastleHealth castle in FindObjectsByType<CastleHealth>(FindObjectsSortMode.None))
        {
            if (DistanceToEdge(castle, center) <= radius)
                castle.TakeDamage(data.castleDamage);
        }
    }

    private static float FlatDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }

    private static float DistanceToEdge(Component target, Vector3 point)
    {
        float closest = FlatDistance(target.transform.position, point);
        foreach (Collider col in target.GetComponentsInChildren<Collider>())
            closest = Mathf.Min(closest, FlatDistance(col.bounds.ClosestPoint(point), point));
        return closest;
    }

    private ParticleSystem CreateFuseSparks()
    {
        FXSettings settings = FXSettings.Instance;
        if (settings == null || settings.SparkMaterial == null) return null;

        var sparksObject = new GameObject("FuseSparks");
        Transform anchor = fusePoint != null ? fusePoint : bomb;
        sparksObject.transform.SetParent(anchor, false);
        sparksObject.transform.localPosition = Vector3.zero;
        sparksObject.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // cone points up

        var system = sparksObject.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = system.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.6f, 1.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.1f);
        main.startColor = new ParticleSystem.MinMaxGradient(settings.sparkColorA, settings.sparkColorB);
        main.gravityModifier = 1.5f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 60;

        var emission = system.emission;
        emission.rateOverTime = 30f;

        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = 0.02f;

        var sizeOverLifetime = system.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // Untextured billboard quads read as square pixels
        var particleRenderer = sparksObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = settings.SparkMaterial;
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;

        system.Play();
        return system;
    }
}
