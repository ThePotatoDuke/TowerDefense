using UnityEngine;

// One-shot burst of glowing pixel sparks, e.g. when two rollers crash into each other.
public static class SparkBurst
{
    private const float MaxLifetime = 0.8f;
    private const float FlashLifetime = 0.1f;

    public static void Play(Vector3 position)
    {
        FXSettings settings = FXSettings.Instance;
        if (settings == null || settings.SparkMaterial == null) return;

        var sparksObject = new GameObject("Sparks");
        sparksObject.transform.position = position;

        var system = sparksObject.AddComponent<ParticleSystem>();
        system.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        var main = system.main;
        main.loop = false;
        main.playOnAwake = false;
        main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, MaxLifetime);
        main.startSpeed = new ParticleSystem.MinMaxCurve(settings.sparkSpeed * 0.4f, settings.sparkSpeed);
        main.startSize = new ParticleSystem.MinMaxCurve(settings.sparkSize * 0.6f, settings.sparkSize);
        main.startColor = new ParticleSystem.MinMaxGradient(settings.sparkColorA, settings.sparkColorB);
        main.gravityModifier = 2f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.maxParticles = 100;

        // Particles are emitted manually below
        var emission = system.emission;
        emission.enabled = false;

        var shape = system.shape;
        shape.shapeType = ParticleSystemShapeType.Hemisphere;
        shape.radius = 0.1f;
        sparksObject.transform.rotation = Quaternion.Euler(-90f, 0f, 0f); // hemisphere opens upward

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
        system.Emit(settings.sparkCount);

        // One big white pixel that blinks at the point of impact
        var flash = new ParticleSystem.EmitParams
        {
            position = position,
            velocity = Vector3.zero,
            startSize = settings.sparkFlashSize,
            startLifetime = FlashLifetime,
            startColor = Color.white,
            applyShapeToPosition = false
        };
        system.Emit(flash, 1);
        Object.Destroy(sparksObject, MaxLifetime + 0.1f);
    }
}
