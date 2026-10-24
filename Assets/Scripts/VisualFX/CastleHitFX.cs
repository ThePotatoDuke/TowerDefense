using DG.Tweening;
using UnityEngine;

// Castle hit feedback: shake, white flash and a burst of pixel debris on every hit,
// plus a pulsing red tint while health is low.
[RequireComponent(typeof(CastleHealth))]
public class CastleHitFX : MonoBehaviour
{
    private CastleHealth castle;
    private SpriteFX spriteFX;
    private Transform visual;
    private Vector3 visualRestPosition;
    private ParticleSystem debris;
    private Tween shakeTween;
    private float lastRatio = 1f;

    private void Awake()
    {
        castle = GetComponent<CastleHealth>();

        SpriteRenderer sprite = GetComponentInChildren<SpriteRenderer>();
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
    }

    private void OnDisable()
    {
        castle.OnHealthChanged -= HandleHealthChanged;
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
