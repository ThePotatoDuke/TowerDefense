using System.Collections;
using DG.Tweening;
using UnityEngine;

// Castle hit feedback: shake, white flash and a burst of pixel debris on every hit,
// plus a pulsing red tint while health is low. When the castle falls it collapses like an old
// Mario castle: it rumbles side to side, then sinks into the ground with dust pouring out at its base.
[RequireComponent(typeof(CastleHealth))]
public class CastleHitFX : MonoBehaviour
{
    private const float ShakeSteps = 30f; // side-to-side flips per second while collapsing

    private CastleHealth castle;
    private SpriteRenderer sprite;
    private SpriteFX spriteFX;
    private Transform visual;
    private Vector3 visualRestPosition;
    private ParticleSystem debris;
    private Tween shakeTween;
    private float lastRatio = 1f;
    private bool collapsing;

    private void Awake()
    {
        castle = GetComponent<CastleHealth>();

        sprite = GetComponentInChildren<SpriteRenderer>();
        if (sprite == null || FXSettings.Instance == null)
        {
            enabled = false;
            return;
        }

        visual = sprite.transform;
        visualRestPosition = visual.localPosition;
        spriteFX = gameObject.GetOrAddComponent<SpriteFX>();
        debris = CreateDebris(sprite.bounds);
    }

    private void OnEnable()
    {
        castle.OnHealthChanged += HandleHealthChanged;
        GameStateManager.OnGameStateChanged += HandleGameStateChanged;
    }

    private void OnDisable()
    {
        castle.OnHealthChanged -= HandleHealthChanged;
        GameStateManager.OnGameStateChanged -= HandleGameStateChanged;
    }

    private void HandleGameStateChanged(GameState state)
    {
        if (state == GameState.CastleDestroyed && !collapsing && enabled)
            StartCoroutine(Collapse());
    }

    private IEnumerator Collapse()
    {
        FXSettings settings = FXSettings.Instance;
        collapsing = true;

        shakeTween?.Kill();
        visual.localPosition = visualRestPosition;

        // Let the camera swing over to the castle first
        yield return new WaitForSeconds(settings.castleCollapseDelay);

        Bounds bounds = sprite.bounds;
        float sinkDistance = bounds.size.y + 0.2f;
        // World "down" in the sprite's parent space, so it sinks straight down whatever the hierarchy
        Vector3 down = visual.parent != null ? visual.parent.InverseTransformDirection(Vector3.down) : Vector3.down;
        Vector3 sideways = visual.parent != null ? visual.parent.InverseTransformDirection(visual.right) : visual.right;

        ParticleSystem dust = CreateDust(bounds);
        CameraShake.Shake(bounds.center, 0.3f, settings.castleCollapseRumble + settings.castleCollapseSinkTime);

        float total = settings.castleCollapseRumble + settings.castleCollapseSinkTime;
        float elapsed = 0f;
        while (elapsed < total)
        {
            // Rumble in place first, then sink, slowly at first
            float sinkProgress = Mathf.Clamp01((elapsed - settings.castleCollapseRumble) / settings.castleCollapseSinkTime);
            float sink = sinkDistance * sinkProgress * sinkProgress;

            // Old-school shake: snap left and right every couple of frames, not a smooth wobble
            float shake = ((int)(elapsed * ShakeSteps) % 2 == 0 ? 1f : -1f) * settings.castleCollapseShake;

            visual.localPosition = visualRestPosition + down * sink + sideways * shake;
            elapsed += Time.deltaTime;
            yield return null;
        }

        visual.gameObject.SetActive(false); // fully buried
        if (dust != null)
            dust.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    // Dust and rubble spilling out along the castle's base for the whole collapse
    private ParticleSystem CreateDust(Bounds spriteBounds)
    {
        FXSettings settings = FXSettings.Instance;
        if (settings.DebrisMaterial == null) return null;

        var dustObject = new GameObject("CastleCollapseDust");
        dustObject.transform.SetParent(transform, false);
        dustObject.transform.position = new Vector3(spriteBounds.center.x, spriteBounds.min.y + 0.05f, spriteBounds.center.z);
        if (Camera.main != null)
            dustObject.transform.rotation = Quaternion.Euler(0f, Camera.main.transform.eulerAngles.y, 0f);

        var system = dustObject.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = system.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.1f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(settings.castleDebrisSize * 0.6f, settings.castleDebrisSize * 1.4f);
        main.startColor = new ParticleSystem.MinMaxGradient(settings.castleDebrisColorA, settings.castleDebrisColorB);
        main.gravityModifier = 0.6f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 400;

        var emission = system.emission;
        emission.rateOverTime = settings.castleCollapseDust;

        // A thin box along the base, spraying up and out
        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(spriteBounds.size.x, 0.05f, 0.5f);
        shape.rotation = new Vector3(-90f, 0f, 0f); // emit upward
        shape.randomDirectionAmount = 0.4f;

        var sizeOverLifetime = system.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        // Untextured billboard quads read as square pixels
        var particleRenderer = dustObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = settings.DebrisMaterial;
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;

        system.Play();
        return system;
    }

    private void Update()
    {
        FXSettings settings = FXSettings.Instance;
        bool lowHealth = lastRatio > 0f && lastRatio <= settings.castleLowHealthThreshold;
        float pulse = lowHealth ? (0.5f + 0.5f * Mathf.Sin(Time.time * 6f)) * 0.6f : 0f;
        spriteFX.SetTint(settings.castleLowHealthColor, pulse);
    }

    private void HandleHealthChanged(float ratio)
    {
        if (collapsing) return;
        if (ratio < lastRatio)
            PlayHit();
        lastRatio = ratio;
    }

    private void PlayHit()
    {
        FXSettings settings = FXSettings.Instance;

        spriteFX.Flash();

        shakeTween?.Kill();
        visual.localPosition = visualRestPosition;
        shakeTween = visual.DOShakePosition(settings.castleShakeDuration, settings.castleShakeStrength, 25, 90f, false, true)
                           .SetLink(gameObject)
                           .OnKill(() => visual.localPosition = visualRestPosition);

        if (debris != null)
            debris.Emit(settings.castleDebrisCount);
    }

    private ParticleSystem CreateDebris(Bounds spriteBounds)
    {
        FXSettings settings = FXSettings.Instance;
        if (settings.DebrisMaterial == null) return null;

        var debrisObject = new GameObject("CastleDebris");
        debrisObject.transform.SetParent(transform, false);
        debrisObject.transform.position = spriteBounds.center;
        debrisObject.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // cone points up

        var system = debrisObject.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = system.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 0.9f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(2.5f, 5f);
        main.startSize = new ParticleSystem.MinMaxCurve(settings.castleDebrisSize * 0.6f, settings.castleDebrisSize);
        main.startColor = new ParticleSystem.MinMaxGradient(settings.castleDebrisColorA, settings.castleDebrisColorB);
        main.gravityModifier = 2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 300;

        // Particles are emitted manually on hit
        var emission = system.emission;
        emission.enabled = false;

        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = 35f;
        shape.radius = Mathf.Max(0.1f, spriteBounds.extents.x * 0.6f);

        var sizeOverLifetime = system.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.2f));

        // Untextured billboard quads read as square pixels
        var particleRenderer = debrisObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = settings.DebrisMaterial;
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;

        system.Play();
        return system;
    }
}
