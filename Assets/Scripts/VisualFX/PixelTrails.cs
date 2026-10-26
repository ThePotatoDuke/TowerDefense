using UnityEngine;

// Pixelated motion trails (PixelTrail shader) behind anything that moves: bats, enemy spikes, projectiles.
public static class PixelTrails
{
    // A fading trail following the parent from the given local point. color tints the material's own
    // color; alpha is the opacity at the head, fading to nothing at the tail.
    public static TrailRenderer Attach(Transform parent, Vector3 localPosition, Material material,
                                       Color color, float time, float width, float alpha = 1f)
    {
        if (material == null) return null;

        var trailObject = new GameObject("PixelTrail");
        trailObject.transform.SetParent(parent, false);
        trailObject.transform.localPosition = localPosition;

        var trail = trailObject.AddComponent<TrailRenderer>();
        trail.sharedMaterial = material;
        trail.time = time;
        // Wider than it looks: the shader leaves an empty inset on each side
        trail.widthMultiplier = width / (1f - 2f * FXSettings.PixelTrailInset);
        trail.widthCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);
        trail.minVertexDistance = 0.05f;
        trail.numCornerVertices = 2;
        trail.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        trail.receiveShadows = false;

        var gradient = new Gradient();
        gradient.SetKeys(
            new[] { new GradientColorKey(color, 0f), new GradientColorKey(color, 1f) },
            new[] { new GradientAlphaKey(alpha, 0f), new GradientAlphaKey(0f, 1f) });
        trail.colorGradient = gradient;
        return trail;
    }

    // Leave the trail behind to fade out on its own, e.g. when its owner is destroyed
    public static void Release(TrailRenderer trail)
    {
        if (trail == null) return;

        trail.transform.SetParent(null, true);
        trail.emitting = false;
        trail.autodestruct = true;
    }
}
