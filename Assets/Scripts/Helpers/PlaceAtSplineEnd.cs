using UnityEngine;
using UnityEngine.Splines;
using Unity.Mathematics;

[ExecuteInEditMode]
public class PlaceAtSplineEnd : MonoBehaviour
{
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField, Range(-1f, 0)] private float offset = 0f;

    void Update()
    {
        if (splineContainer == null)
            return;

        var spline = splineContainer.Spline;

        // Clamp t + offset into valid range [0,1]
        float t = Mathf.Clamp01(1f + offset);

        // Get position/rotation in local space
        float3 pos = spline.EvaluatePosition(t);
        float3 tangent = spline.EvaluateTangent(t);
        float3 up = spline.EvaluateUpVector(t);

        // Convert to world
        Matrix4x4 localToWorld = splineContainer.transform.localToWorldMatrix;
        Vector3 worldPos = localToWorld.MultiplyPoint3x4(pos);
        Vector3 worldTangent = localToWorld.MultiplyVector(tangent);
        Vector3 worldUp = localToWorld.MultiplyVector(up);

        Quaternion rot = Quaternion.LookRotation(worldTangent, worldUp);

        transform.SetPositionAndRotation(worldPos, rot);
    }
}
