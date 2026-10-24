using UnityEngine;

// Dashed pixel lines lying on the ground, e.g. to warn where something is about to fly.
// Dashes are the size of sprite pixels; scroll them with MarchDashes to make them run along the line.
public static class GroundLine
{
    private const float PixelsPerUnit = 16f; // matches the sprites
    private const int DashPixels = 3;
    private const int GapPixels = 2;
    private const float WidthPixels = 2f;

    private static Texture2D dashTexture;

    // A line from start along direction (flat on the ground) for the given length.
    // The material is made for this line: animate its color and dashes, and destroy both when done.
    public static Transform Create(Vector3 start, Vector3 direction, float length, Color color, out Material material)
    {
        material = null;
        FXSettings settings = FXSettings.Instance;
        if (settings == null || settings.unlitShader == null) return null;

        direction.y = 0f;
        direction.Normalize();

        material = new Material(settings.unlitShader) { name = "GroundLine (Runtime)" };
        material.mainTexture = GetDashTexture();
        material.SetColor("_Color", color);
        // One dash pattern every (dash + gap) sprite pixels along the line
        material.mainTextureScale = new Vector2(length * PixelsPerUnit / (DashPixels + GapPixels), 1f);

        var line = new GameObject("GroundLine");
        line.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        var lineRenderer = line.AddComponent<MeshRenderer>();
        lineRenderer.sharedMaterial = material;
        lineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        lineRenderer.receiveShadows = false;

        // Lie flat, with the quad's X axis running along the direction
        float yaw = Mathf.Atan2(-direction.z, direction.x) * Mathf.Rad2Deg;
        line.transform.SetPositionAndRotation(
            start + direction * (length * 0.5f) + Vector3.up * 0.03f,
            Quaternion.Euler(0f, yaw, 0f) * Quaternion.Euler(90f, 0f, 0f));
        line.transform.localScale = new Vector3(length, WidthPixels / PixelsPerUnit, 1f);
        return line.transform;
    }

    // Scroll the dashes outward along the line, by whole sprite pixels so they stay crisp
    public static void MarchDashes(Material material, float elapsed, float pixelsPerSecond)
    {
        float pixels = Mathf.Floor(elapsed * pixelsPerSecond);
        material.mainTextureOffset = new Vector2(-pixels / (DashPixels + GapPixels), 0f);
    }

    private static Texture2D GetDashTexture()
    {
        if (dashTexture != null) return dashTexture;

        dashTexture = new Texture2D(DashPixels + GapPixels, 1, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Repeat,
            name = "GroundLineDash"
        };
        for (int x = 0; x < DashPixels + GapPixels; x++)
            dashTexture.SetPixel(x, 0, x < DashPixels ? Color.white : Color.clear);
        dashTexture.Apply();
        return dashTexture;
    }
}
