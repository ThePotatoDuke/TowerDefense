using UnityEngine;
using UnityEngine.Splines;

// Spots on the ground around the castle, measured from the end of the enemy path (where enemies stop to
// attack): "back" runs along the path toward where enemies come from, "side" across it. Also how far a
// point is from the path, for planting beside it.
public static class CastleGround
{
    private static SplineContainer path;

    private static SplineContainer Path
    {
        get
        {
            if (path == null)
                path = Object.FindAnyObjectByType<SplineContainer>();
            return path;
        }
    }

    // The end of the path, where enemies gather at the castle
    public static Vector3 End => Path != null ? (Vector3)Path.EvaluatePosition(1f) : Vector3.zero;

    // A point back along the path from the castle and out to the side, dropped onto the ground
    public static Vector3 Spot(float back, float side)
    {
        if (Path == null) return Vector3.zero;

        Vector3 end = Path.EvaluatePosition(1f);
        Vector3 forward = Path.EvaluateTangent(1f);
        forward.y = 0f;
        forward = forward.sqrMagnitude > 0.0001f ? forward.normalized : Vector3.forward;
        Vector3 sideways = Vector3.Cross(Vector3.up, forward);

        return OnGround(end - forward * back + sideways * side);
    }

    public static Vector3 Spot(Vector2 backAndSide) => Spot(backAndSide.x, backAndSide.y);

    // Distance across the ground from the point to the middle of the path
    public static float DistanceToPath(Vector3 point)
    {
        if (Path == null) return float.MaxValue;

        Vector3 local = Path.transform.InverseTransformPoint(point);
        SplineUtility.GetNearestPoint(Path.Spline, local, out var nearest, out _);
        Vector3 offset = Path.transform.TransformPoint(nearest) - point;
        offset.y = 0f;
        return offset.magnitude;
    }

    // The lowest surface under the point, so enemies or the player standing there don't count as ground
    public static Vector3 OnGround(Vector3 point)
    {
        RaycastHit[] hits = Physics.RaycastAll(point + Vector3.up * 5f, Vector3.down, 20f,
                                               Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        if (hits.Length == 0) return point;

        float lowest = float.MaxValue;
        foreach (RaycastHit hit in hits)
            lowest = Mathf.Min(lowest, hit.point.y);
        point.y = lowest;
        return point;
    }
}
