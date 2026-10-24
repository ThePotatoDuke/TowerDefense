using UnityEngine;

// Motion tail behind a flying enemy: a pixelated, fading ribbon that traces its path through
// the air, plus a few square pixels shed along the way. Both render behind sprites.
public class BatTrail : MonoBehaviour
{
    [Tooltip("Sprite the trail comes from. Defaults to the first SpriteRenderer in children.")]
    [SerializeField] private SpriteRenderer source;

    private TrailRenderer trail;
    private ParticleSystem pixels;
    private EnemyBase enemy;

    // Set before Start (BatEnemy does it in Awake)
    public Color Color { get; set; } = Color.white;

    private void Start()
    {
        FXSettings settings = FXSettings.Instance;
        if (source == null)
            source = GetComponentInChildren<SpriteRenderer>();
        if (source == null || settings == null || settings.BatTrailMaterial == null || settings.BatPixelsMaterial == null)
        {
            enabled = false;
            return;
        }

        // Separate objects: a GameObject can only have one renderer
        trail = CreateTrail(CreateEmitter("BatTrail"), settings);
        pixels = CreatePixels(CreateEmitter("BatPixels"), settings);

        enemy = GetComponent<EnemyBase>();
        if (enemy != null)
            enemy.OnDied += Release;
    }

    private void OnDestroy()
    {
        if (enemy != null)
            enemy.OnDied -= Release;
    }

    // On death, leave the trail and pixels behind to fade out on their own
    private void Release()
    {
        if (trail == null) return;

        trail.transform.SetParent(null, true);
        trail.emitting = false;
        trail.autodestruct = true;
        trail = null;

        pixels.transform.SetParent(null, true);
        pixels.Stop(true, ParticleSystemStopBehavior.StopEmitting);
        pixels = null;
    }

    // At the middle of the sprite, following it as it flips and bobs
    private GameObject CreateEmitter(string label)
    {
        var emitter = new GameObject(label);
        emitter.transform.SetParent(source.transform, false);
        emitter.transform.localPosition = source.localBounds.center;
        return emitter;
    }

    private TrailRenderer CreateTrail(GameObject owner, FXSettings settings)
    {
        var ribbon = owner.AddComponent<TrailRenderer>();
        ribbon.sharedMaterial = settings.BatTrailMaterial;
        ribbon.time = settings.batTrailTime;
        // Wider than it looks: the shader leaves an empty inset on each side
        ribbon.widthMultiplier = settings.batTrailWidth / (1f - 2f * FXSettings.BatTrailInset);
        ribbon.widthCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        ribbon.minVertexDistance = 0.05f;
        ribbon.numCornerVertices = 2;
        ribbon.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        ribbon.receiveShadows = false;

        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color, 0f), new GradientColorKey(Color, 1f) },
            new[] { new GradientAlphaKey(settings.batTrailAlpha, 0f), new GradientAlphaKey(0f, 1f) });
        ribbon.colorGradient = gradient;
        return ribbon;
    }

    private ParticleSystem CreatePixels(GameObject owner, FXSettings settings)
    {
        var particles = owner.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(settings.batPixelLifetime * 0.6f, settings.batPixelLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0f, 0.3f);
        main.startSize = new ParticleSystem.MinMaxCurve(settings.batPixelSize * 0.6f, settings.batPixelSize);
        main.startColor = new ParticleSystem.MinMaxGradient(Color, Color.Lerp(Color, Color.black, 0.4f));
        main.gravityModifier = 0.15f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 100;
        main.stopAction = ParticleSystemStopAction.Destroy; // cleans up after Release

        // Emit by distance flown, so the amount is even at any speed
        var emission = particles.emission;
        emission.rateOverTime = 0f;
        emission.rateOverDistance = settings.batPixelDensity;

        var shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.08f;

        var sizeOverLifetime = particles.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // Untextured billboard quads read as square pixels
        var particleRenderer = owner.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = settings.BatPixelsMaterial;
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;

        particles.Play();
        return particles;
    }
}
