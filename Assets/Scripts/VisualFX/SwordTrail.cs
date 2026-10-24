using UnityEngine;

// White swoosh trail along the outer part of a sword blade, emitted only while swinging.
public class SwordTrail : MonoBehaviour
{
    private TrailRenderer trail;

    // The blade collider's local Z axis runs along the blade
    public void Setup(BoxCollider blade)
    {
        FXSettings settings = FXSettings.Instance;
        if (blade == null || settings == null || settings.SwooshMaterial == null) return;

        float bladeLength = blade.size.z * blade.transform.lossyScale.z;

        // Sit toward the tip so the swoosh covers the outer arc of the swing
        var trailObject = new GameObject("Swoosh");
        trailObject.transform.SetParent(blade.transform, false);
        trailObject.transform.localPosition = blade.center + Vector3.forward * (blade.size.z * 0.2f);

        trail = trailObject.AddComponent<TrailRenderer>();
        trail.sharedMaterial = settings.SwooshMaterial;
        trail.time = settings.swooshTime;
        trail.widthMultiplier = bladeLength * settings.swooshWidth;
        trail.widthCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.1f);
        trail.colorGradient = FXSettings.FadeOutGradient();
        trail.minVertexDistance = 0.02f;
        trail.numCornerVertices = 2;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;
        trail.emitting = false;
    }

    public void Begin()
    {
        if (trail == null) return;
        trail.Clear(); // avoid a streak from where the previous swing ended
        trail.emitting = true;
    }

    public void End()
    {
        if (trail == null) return;
        trail.emitting = false;
    }
}
