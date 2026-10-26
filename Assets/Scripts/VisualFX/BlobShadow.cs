using UnityEngine;

// Soft pixel shadow projected onto the ground under a sprite or model.
// Shrinks and fades as the object rises (e.g. slime hops), which grounds 2D sprites in the 3D world.
public class BlobShadow : MonoBehaviour
{
    private static readonly int AlphaId = Shader.PropertyToID("_Alpha");
    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly RaycastHit[] hits = new RaycastHit[8];

    // Height changes below this are ignored, so walk bobs don't make the shadow shimmer
    private const float HeightDeadZone = 0.2f;

    [Tooltip("Renderer the shadow follows. Defaults to the first SpriteRenderer in children.")]
    [SerializeField] private Renderer source;

    private Transform shadow;
    private MeshRenderer shadowRenderer;
    private bool sized;
    private Material material;
    private Vector3 groundPoint;
    private bool hasGround;

    public Renderer Source { set => source = value; }

    private void Start()
    {
        FXSettings settings = FXSettings.Instance;
        if (source == null)
            source = GetComponentInChildren<SpriteRenderer>();
        if (source == null || settings == null || settings.blobShadowShader == null)
        {
            enabled = false;
            return;
        }

        material = new Material(settings.blobShadowShader) { name = "BlobShadow (Instance)" };
        material.SetColor("_Color", settings.shadowColor);

        var shadowObject = new GameObject("BlobShadow");
        shadow = shadowObject.transform;
        shadow.SetParent(transform, false);

        shadowObject.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        shadowRenderer = shadowObject.AddComponent<MeshRenderer>();
        shadowRenderer.sharedMaterial = material;
        shadowRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        shadowRenderer.receiveShadows = false;

        TrySize();
    }

    // Fixed size, measured once: billboard bounds change as the sprite turns toward the camera.
    // Height only shrinks the blob inside the quad, so its pixel grid never rescales.
    // Waits while the object is still scaling up from nothing (spawning): measured at a tiny scale, the
    // shadow would come out enormous once the object reached full size.
    private bool TrySize()
    {
        float scale = transform.lossyScale.x;
        if (scale < 0.5f)
        {
            shadowRenderer.enabled = false;
            return false;
        }

        float size = Mathf.Max(0.3f, source.bounds.size.x * FXSettings.Instance.shadowSizeMultiplier);
        shadow.localScale = Vector3.one * size / scale;
        shadowRenderer.enabled = true;
        sized = true;
        return true;
    }

    private void LateUpdate()
    {
        if (!sized && !TrySize()) return;

        Vector3 feet = GetFeetPosition();

        // Horizontal position blends from the feet toward the sprite's center
        Vector3 center = source.bounds.center;
        Vector3 anchor = Vector3.Lerp(feet, center, FXSettings.Instance.shadowAnchor);
        anchor.y = feet.y;

        // Keep the last ground point if the ray misses for a frame instead of popping off
        if (TryFindGround(anchor + Vector3.up * 0.5f, out Vector3 point))
        {
            groundPoint = point;
            hasGround = true;
        }
        if (!hasGround) return;

        float height = Mathf.Max(0f, feet.y - groundPoint.y - HeightDeadZone);
        float t = Mathf.Clamp01(height / FXSettings.Instance.shadowMaxHeight);

        shadow.SetPositionAndRotation(groundPoint + Vector3.up * 0.05f, Quaternion.Euler(90f, 0f, 0f));
        material.SetFloat(RadiusId, Mathf.Lerp(1f, 0.6f, t));
        material.SetFloat(AlphaId, Mathf.Lerp(1f, 0.35f, t));
    }

    private void OnDestroy()
    {
        if (material != null)
            Destroy(material);
    }

    // Sprites are tilted toward the camera, so the middle of their world bounds isn't over
    // their feet. Use the bottom edge of the sprite itself; models use their bounds' base.
    private Vector3 GetFeetPosition()
    {
        if (source is SpriteRenderer)
        {
            Bounds local = source.localBounds;
            return source.transform.TransformPoint(new Vector3(local.center.x, local.min.y, local.center.z));
        }

        Bounds bounds = source.bounds;
        return new Vector3(bounds.center.x, bounds.min.y, bounds.center.z);
    }

    // Nearest static surface below, ignoring this object, characters and triggers
    private bool TryFindGround(Vector3 origin, out Vector3 point)
    {
        int count = Physics.RaycastNonAlloc(origin, Vector3.down, hits, 20f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);

        point = default;
        float closest = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = hits[i];
            if (hit.transform.IsChildOf(transform)) continue;
            if (hit.collider.attachedRigidbody != null) continue;
            if (hit.collider.GetComponentInParent<EnemyBase>() != null) continue;

            if (hit.distance < closest)
            {
                closest = hit.distance;
                point = hit.point;
            }
        }
        return closest < float.MaxValue;
    }
}
