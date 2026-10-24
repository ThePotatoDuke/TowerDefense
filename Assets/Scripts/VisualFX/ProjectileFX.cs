using UnityEngine;

// Makes a projectile glow (HDR emission picked up by bloom) and leave a short trail.
public class ProjectileFX : MonoBehaviour
{
    private TrailRenderer trail;

    private void Awake()
    {
        FXSettings settings = FXSettings.Instance;
        if (settings == null) return;

        gameObject.GetOrAddComponent<SpriteFX>().SetEmission(settings.projectileEmission);

        Material trailMaterial = settings.ProjectileTrailMaterial;
        if (trailMaterial == null) return;

        var trailObject = new GameObject("Trail");
        trailObject.transform.SetParent(transform, false);

        trail = trailObject.AddComponent<TrailRenderer>();
        trail.sharedMaterial = trailMaterial;
        trail.time = settings.projectileTrailTime;
        trail.widthMultiplier = settings.projectileTrailWidth;
        trail.widthCurve = AnimationCurve.Linear(0f, 1f, 1f, 0f);
        trail.colorGradient = FXSettings.FadeOutGradient();
        trail.minVertexDistance = 0.05f;
        trail.numCapVertices = 2;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
    }

    // Leave the trail behind to fade out when the projectile is destroyed on impact
    public void ReleaseTrail()
    {
        if (trail == null) return;

        trail.transform.SetParent(null, true);
        trail.emitting = false;
        trail.autodestruct = true;
        trail = null;
    }
}
