using UnityEngine;

// Lays the ground's texture out in world units instead of stretching it over the mesh: one tile covers
// the same patch of world whatever the ground's size or scale, and two grounds that both have this line
// up seamlessly where they meet. The default size makes a 32 px texture match the 16 pixels-per-unit
// sprites. Works on any flat ground mesh lying level (e.g. Unity's Plane), turned to any multiple of 90°.
// Set per renderer, so grounds can share one material.
[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class WorldTiling : MonoBehaviour
{
    private static readonly int BaseMapST = Shader.PropertyToID("_BaseMap_ST");
    private static readonly int MainTexST = Shader.PropertyToID("_MainTex_ST");

    [Tooltip("World units one tile of the texture covers. Texture width in pixels / 16 matches the sprites.")]
    [SerializeField] private float tileSize = 2f;
    [Tooltip("Shifts the pattern, in world units, if it should line up differently.")]
    [SerializeField] private Vector2 offset;

    private Renderer target;
    private MaterialPropertyBlock block;
    private Matrix4x4 appliedFor;
    private float appliedSize = -1f;
    private Vector2 appliedOffset;

    private void OnEnable()
    {
        target = GetComponent<Renderer>();
        Apply();
    }

    private void OnDisable()
    {
        // Back to the material's own tiling
        if (target != null)
            target.SetPropertyBlock(null);
        appliedSize = -1f;
    }

    private void OnValidate() => appliedSize = -1f;

    private void Update()
    {
        if (transform.localToWorldMatrix != appliedFor || tileSize != appliedSize || offset != appliedOffset)
            Apply();
    }

    private void Apply()
    {
        var filter = GetComponent<MeshFilter>();
        Mesh mesh = filter != null ? filter.sharedMesh : null;
        if (target == null || mesh == null || tileSize <= 0f) return;

        appliedFor = transform.localToWorldMatrix;
        appliedSize = tileSize;
        appliedOffset = offset;

        // Find how the mesh's own UVs run across the world: two points that differ only in U, two only in V
        Vector3[] vertices = mesh.vertices;
        Vector2[] uvs = mesh.uv;
        if (vertices.Length < 3 || uvs.Length != vertices.Length) return;

        if (!TryAxis(vertices, uvs, 0, out float scaleU, out float offsetU) ||
            !TryAxis(vertices, uvs, 1, out float scaleV, out float offsetV))
            return;

        var st = new Vector4(scaleU, scaleV, offsetU, offsetV);
        block ??= new MaterialPropertyBlock();
        target.GetPropertyBlock(block);
        block.SetVector(BaseMapST, st);
        block.SetVector(MainTexST, st);
        target.SetPropertyBlock(block);
    }

    // Tiling and offset for one UV channel (0 = U, 1 = V) so that it counts tiles along whichever world
    // axis (X or Z) it runs along: uv' = uv * scale + offset = world / tileSize
    private bool TryAxis(Vector3[] vertices, Vector2[] uvs, int channel, out float scale, out float shift)
    {
        scale = 1f;
        shift = 0f;
        int other = 1 - channel;
        Vector2 uv0 = uvs[0];

        for (int i = 1; i < vertices.Length; i++)
        {
            Vector2 uv1 = uvs[i];
            if (Mathf.Abs(uv1[other] - uv0[other]) > 0.0001f || Mathf.Abs(uv1[channel] - uv0[channel]) < 0.0001f)
                continue;

            Vector3 world0 = transform.TransformPoint(vertices[0]);
            Vector3 world1 = transform.TransformPoint(vertices[i]);
            Vector3 step = world1 - world0;
            bool alongX = Mathf.Abs(step.x) >= Mathf.Abs(step.z);
            float w0 = (alongX ? world0.x + offset.x : world0.z + offset.y) / tileSize;
            float w1 = (alongX ? world1.x + offset.x : world1.z + offset.y) / tileSize;

            scale = (w1 - w0) / (uv1[channel] - uv0[channel]);
            shift = w0 - uv0[channel] * scale;
            return true;
        }
        return false;
    }
}
