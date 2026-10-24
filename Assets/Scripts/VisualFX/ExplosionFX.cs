using DG.Tweening;
using UnityEngine;

// Pixel explosion: a blink of white at the center, a burst of glowing fire squares, a puff of
// smoke that drifts up, a pixelated shockwave dome and ring that sweep out to the edge of the blast,
// and a camera shake. Everything scales with the blast radius. Also makes blast-radius ground rings.
public static class ExplosionFX
{
    private const float FlashLifetime = 0.12f;
    private const float SmokeLifetime = 1.6f;
    private const float ShockwaveExpand = 0.3f;
    private const float ShockwaveFade = 0.6f;
    private const float PixelsPerUnit = 16f; // matches the sprites

    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    private static readonly int RingId = Shader.PropertyToID("_Ring");
    private static readonly int TexelsId = Shader.PropertyToID("_Texels");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private static readonly Color FireA = new Color(1f, 0.9f, 0.45f, 1f);
    private static readonly Color FireB = new Color(1f, 0.35f, 0.1f, 1f);
    private static readonly Color SmokeA = new Color(0.35f, 0.33f, 0.35f, 0.9f);
    private static readonly Color SmokeB = new Color(0.18f, 0.17f, 0.2f, 0.9f);

    // position: middle of the blast; groundY: height of the ground beneath it, where the shockwave sits
    public static void Play(Vector3 position, float radius, float groundY)
    {
        FXSettings settings = FXSettings.Instance;
        if (settings == null) return;

        var root = new GameObject("Explosion");
        root.transform.position = position;

        if (settings.SparkMaterial != null)
            PlayFire(root.transform, settings.SparkMaterial, radius);
        if (settings.DebrisMaterial != null)
            PlaySmoke(root.transform, settings.DebrisMaterial, radius);
        if (settings.shockwaveShader != null)
            PlayShockwave(root.transform, settings.shockwaveShader, new Vector3(position.x, groundY, position.z), radius);

        CameraShake.Shake(position, 0.4f + radius * 0.15f, 0.5f);
        Object.Destroy(root, SmokeLifetime + 0.2f);
    }

    // A flat pixel ring on the ground covering the given radius, e.g. to warn where a blast will reach.
    // Animate it through the returned material (_Alpha, _Sweep, _Color); destroy the object when done.
    public static Transform CreateGroundRing(Vector3 groundPoint, float radius, Color color, out Material material)
    {
        material = null;
        FXSettings settings = FXSettings.Instance;
        if (settings == null || settings.shockwaveShader == null) return null;

        material = CreateShockwaveMaterial(settings.shockwaveShader, "GroundRing", true);
        material.SetColor(ColorId, color);
        Transform ring = CreateShockwavePart(null, "GroundRing", material, groundPoint + Vector3.up * 0.04f, Quaternion.Euler(90f, 0f, 0f));
        SetSize(ring, material, radius * 2f);
        return ring;
    }

    // Grows a shockwave part and keeps its pixels the size of sprite pixels as it does
    public static void SetSize(Transform part, Material material, float diameter)
    {
        part.localScale = Vector3.one * diameter;
        material.SetFloat(TexelsId, Mathf.Max(4f, Mathf.Round(diameter * PixelsPerUnit)));
    }

    // A glowing dome and a ring on the ground that sweep out to the edge of the blast, showing its reach
    private static void PlayShockwave(Transform parent, Shader shader, Vector3 groundPoint, float radius)
    {
        float diameter = radius * 2f;

        // Dome: a disc facing the camera, centered on the ground so the ground hides its lower half
        Quaternion facing = Camera.main != null ? Camera.main.transform.rotation : Quaternion.identity;
        Material domeMaterial = CreateShockwaveMaterial(shader, "ShockwaveDome", false);
        Transform dome = CreateShockwavePart(parent, "ShockwaveDome", domeMaterial, groundPoint, facing);
        Expand(dome, domeMaterial, diameter);
        FadeAndRelease(domeMaterial, dome, ShockwaveExpand * 0.5f);

        // Ring: flat, just above the ground, facing up
        Material ringMaterial = CreateShockwaveMaterial(shader, "ShockwaveRing", true);
        Transform ring = CreateShockwavePart(parent, "ShockwaveRing", ringMaterial,
                                             groundPoint + Vector3.up * 0.04f, Quaternion.Euler(90f, 0f, 0f));
        Expand(ring, ringMaterial, diameter);
        FadeAndRelease(ringMaterial, ring, ShockwaveExpand * 0.7f);
    }

    private static void Expand(Transform part, Material material, float diameter)
    {
        SetSize(part, material, 0f);
        DOVirtual.Float(0f, diameter, ShockwaveExpand, size => SetSize(part, material, size))
                 .SetEase(Ease.OutCubic)
                 .SetLink(part.gameObject);
    }

    // Fade the part out, then hide it and free its one-off material
    private static void FadeAndRelease(Material material, Transform part, float delay)
    {
        material.DOFloat(0f, AlphaId, ShockwaveFade)
                .SetDelay(delay)
                .SetLink(part.gameObject)
                .OnKill(() =>
                {
                    if (part != null)
                        part.gameObject.SetActive(false);
                    Object.Destroy(material);
                });
    }

    private static Material CreateShockwaveMaterial(Shader shader, string label, bool ring)
    {
        var material = new Material(shader) { name = $"{label} (Runtime)" };
        material.SetFloat(RingId, ring ? 1f : 0f);
        return material;
    }

    private static Transform CreateShockwavePart(Transform parent, string label, Material material,
                                                 Vector3 position, Quaternion rotation)
    {
        var part = new GameObject(label);
        part.transform.SetParent(parent, false);
        part.transform.SetPositionAndRotation(position, rotation);
        part.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");

        var partRenderer = part.AddComponent<MeshRenderer>();
        partRenderer.sharedMaterial = material;
        partRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        partRenderer.receiveShadows = false;
        return part.transform;
    }

    private static void PlayFire(Transform parent, Material material, float radius)
    {
        ParticleSystem system = CreateSystem(parent, "Fire", material);

        var main = system.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(radius * 1.5f, radius * 3.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.12f, 0.28f);
        main.startColor = new ParticleSystem.MinMaxGradient(FireA, FireB);
        main.gravityModifier = 0.5f;

        // Fast burst that slows down quickly, so the fire fills the blast area instead of flying off
        var drag = system.limitVelocityOverLifetime;
        drag.enabled = true;
        drag.limit = 100f; // no hard speed cap, only the drag
        drag.drag = 6f;

        var colorOverLifetime = system.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, 0.4f, 0.2f), 1f) },
            new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = gradient;

        system.Play();
        system.Emit(Mathf.RoundToInt(30 + radius * 15));

        // One big white blink at the center
        system.Emit(new ParticleSystem.EmitParams
        {
            position = parent.position,
            velocity = Vector3.zero,
            startSize = radius * 1.4f,
            startLifetime = FlashLifetime,
            startColor = Color.white,
            applyShapeToPosition = false
        }, 1);
    }

    private static void PlaySmoke(Transform parent, Material material, float radius)
    {
        ParticleSystem system = CreateSystem(parent, "Smoke", material);

        var main = system.main;
        main.startLifetime = new ParticleSystem.MinMaxCurve(SmokeLifetime * 0.6f, SmokeLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(radius * 0.3f, radius * 0.9f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
        main.startColor = new ParticleSystem.MinMaxGradient(SmokeA, SmokeB);
        main.gravityModifier = -0.15f; // drifts up

        var colorOverLifetime = system.colorOverLifetime;
        colorOverLifetime.enabled = true;
        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
            new[] { new GradientAlphaKey(0f, 0f), new GradientAlphaKey(1f, 0.15f), new GradientAlphaKey(0f, 1f) });
        colorOverLifetime.color = gradient;

        system.Play();
        system.Emit(Mathf.RoundToInt(10 + radius * 5));
    }

    private static ParticleSystem CreateSystem(Transform parent, string label, Material material)
    {
        var particleObject = new GameObject(label);
        particleObject.transform.SetParent(parent, false);

        var system = particleObject.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = system.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 200;

        // Particles are emitted by hand
        var emission = system.emission;
        emission.enabled = false;

        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.2f;

        var sizeOverLifetime = system.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0.3f));

        // Untextured billboard quads read as square pixels
        var particleRenderer = particleObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = material;
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;

        return system;
    }
}
