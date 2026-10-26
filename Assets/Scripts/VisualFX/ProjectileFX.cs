using UnityEngine;

// Makes a projectile glow (HDR emission picked up by bloom) and leave a short pixelated trail.
public class ProjectileFX : MonoBehaviour
{
    private TrailRenderer trail;

    private void Awake()
    {
        FXSettings settings = FXSettings.Instance;
        if (settings == null) return;

        gameObject.GetOrAddComponent<SpriteFX>().SetEmission(settings.projectileEmission);

        // The material carries the glowing trail color; the vertex color stays white
        trail = PixelTrails.Attach(transform, Vector3.zero, settings.ProjectileTrailMaterial, Color.white,
                                   settings.projectileTrailTime, settings.projectileTrailWidth);
    }

    // Leave the trail behind to fade out when the projectile is destroyed on impact
    public void ReleaseTrail()
    {
        PixelTrails.Release(trail);
        trail = null;
    }
}
