using UnityEngine;

// One-shot puff of drifting pixel spores, e.g. when a Spore Mine bursts. Like SparkBurst, but slower,
// bigger and floating up instead of falling.
public static class SporeCloud
{
    private const float MaxLifetime = 1.4f;
    private const int Count = 26;

    public static void Play(Vector3 position, Color colorA, Color colorB)
    {
        FXSettings settings = FXSettings.Instance;
        if (settings == null || settings.SparkMaterial == null) return;

        var cloudObject = new GameObject("Spores");
        cloudObject.transform.position = position;

        var system = cloudObject.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = system.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.7f, MaxLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.8f, 2.6f);
        main.startSize = new ParticleSystem.MinMaxCurve(settings.sparkSize * 0.8f, settings.sparkSize * 1.8f);
        main.startColor = new ParticleSystem.MinMaxGradient(colorA, colorB);
        main.gravityModifier = -0.15f; // drifts up
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = Count;

        var emission = system.emission;
        emission.enabled = false;

        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.25f;

        // Slows down as it spreads, like a puff of dust
        var limit = system.limitVelocityOverLifetime;
        limit.enabled = true;
        limit.limit = 0.4f;
        limit.dampen = 0.15f;

        var sizeOverLifetime = system.sizeOverLifetime;
        sizeOverLifetime.enabled = true;
        sizeOverLifetime.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));

        // Untextured billboard quads read as square pixels
        var particleRenderer = cloudObject.GetComponent<ParticleSystemRenderer>();
        particleRenderer.sharedMaterial = settings.SparkMaterial;
        particleRenderer.renderMode = ParticleSystemRenderMode.Billboard;
        particleRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        particleRenderer.receiveShadows = false;

        system.Play();
        system.Emit(Count);
        Object.Destroy(cloudObject, MaxLifetime + 0.1f);
    }
}
