using UnityEngine;

// Gives the flat ground some thickness: four walls hanging down from its edges, deep enough that the
// camera never sees their bottom. Runs in the editor too and rebuilds whenever the ground is moved or
// resized. Textured in world units, so the side texture never stretches whatever the ground's size.
[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class GroundSkirt : MonoBehaviour
{
    [Tooltip("The ground whose edges the walls hang from.")]
    [SerializeField] private Renderer ground;
    [Tooltip("How far the walls reach down below the ground.")]
    [SerializeField] private float depth = 12f;
    [Tooltip("Texture tiles per unit. 1 makes a 16 px texture match the 16 PPU sprites.")]
    [SerializeField] private float tilesPerUnit = 1f;

    private Mesh mesh;
    private Bounds builtBounds;
    private float builtDepth = -1f;
    private float builtTiles = -1f;

    private void OnEnable()
    {
        Rebuild();
    }

    private void OnDisable()
    {
        if (mesh == null) return;
        if (Application.isPlaying)
            Destroy(mesh);
        else
            DestroyImmediate(mesh);
        mesh = null;
    }

    private void Update()
    {
        if (ground == null) return;
        if (ground.bounds != builtBounds || depth != builtDepth || tilesPerUnit != builtTiles)
            Rebuild();
    }

    private void Rebuild()
    {
        if (ground == null) return;

        builtBounds = ground.bounds;
        builtDepth = depth;
        builtTiles = tilesPerUnit;

        if (mesh == null)
        {
            // Rebuilt from the ground every time, so it's never saved with the scene
            mesh = new Mesh { name = "GroundSkirt", hideFlags = HideFlags.DontSave };
            GetComponent<MeshFilter>().sharedMesh = mesh;
        }

        Bounds b = builtBounds;
        float top = b.max.y;
        float bottom = top - depth;

        // Corners in order around the ground, so each wall's outside is its front face
        var corners = new[]
        {
            new Vector3(b.min.x, 0f, b.min.z),
            new Vector3(b.max.x, 0f, b.min.z),
            new Vector3(b.max.x, 0f, b.max.z),
            new Vector3(b.min.x, 0f, b.max.z)
        };

        var vertices = new Vector3[16];
        var uvs = new Vector2[16];
        var triangles = new int[24];
        float along = 0f;

        for (int side = 0; side < 4; side++)
        {
            Vector3 start = corners[side];
            Vector3 end = corners[(side + 1) % 4];
            float length = Vector3.Distance(start, end);
            int v = side * 4;

            vertices[v] = WorldToLocal(start, top);
            vertices[v + 1] = WorldToLocal(end, top);
            vertices[v + 2] = WorldToLocal(end, bottom);
            vertices[v + 3] = WorldToLocal(start, bottom);

            // World-space UVs: one tile per unit along the wall and down it, top edge lined up with the ground
            uvs[v] = new Vector2(along, 0f) * tilesPerUnit;
            uvs[v + 1] = new Vector2(along + length, 0f) * tilesPerUnit;
            uvs[v + 2] = new Vector2(along + length, -depth) * tilesPerUnit;
            uvs[v + 3] = new Vector2(along, -depth) * tilesPerUnit;
            along += length;

            int t = side * 6;
            triangles[t] = v;
            triangles[t + 1] = v + 1;
            triangles[t + 2] = v + 2;
            triangles[t + 3] = v;
            triangles[t + 4] = v + 2;
            triangles[t + 5] = v + 3;
        }

        mesh.Clear();
        mesh.vertices = vertices;
        mesh.uv = uvs;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }

    private Vector3 WorldToLocal(Vector3 corner, float height)
    {
        return transform.InverseTransformPoint(new Vector3(corner.x, height, corner.z));
    }
}
