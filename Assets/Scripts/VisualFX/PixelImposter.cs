using UnityEngine;
using UnityEngine.Rendering.Universal;

// Renders a 3D model as outlined pixel art so it matches the sprites.
// A private orthographic camera renders the model into a tiny RenderTexture every frame;
// a camera-facing quad shows that texture with point filtering and an outline.
// The model itself moves to an unused layer that the main camera no longer draws.
public class PixelImposter : MonoBehaviour
{
    private static readonly int TexSizeId = Shader.PropertyToID("_TexSize");
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");

    private Renderer[] sources;
    private Camera imposterCamera;
    private RenderTexture renderTexture;
    private Material quadMaterial;
    private Transform quad;
    private float halfSize;
    private int layerMask;

    private void Awake()
    {
        FXSettings settings = FXSettings.Instance;
        sources = GetComponentsInChildren<MeshRenderer>();
        if (sources.Length == 0 || settings == null || settings.pixelImposterShader == null)
        {
            enabled = false;
            return;
        }

        int layer = settings.pixelArtLayer;
        layerMask = 1 << layer;
        foreach (Renderer source in sources)
            source.gameObject.layer = layer;

        // Square frame big enough for the model at any rotation, plus room for the outline
        Bounds bounds = GetBounds();
        float maxExtent = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z));
        halfSize = maxExtent * 1.25f;
        int resolution = Mathf.Clamp(Mathf.CeilToInt(halfSize * 2f * settings.imposterPixelsPerUnit), 8, 256);

        renderTexture = new RenderTexture(resolution, resolution, 16, RenderTextureFormat.ARGB32)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            name = $"{name} Imposter"
        };
        renderTexture.Create();

        CreateCamera();
        CreateQuad(settings, resolution);
    }

    private void CreateCamera()
    {
        var cameraObject = new GameObject("ImposterCamera");
        cameraObject.transform.SetParent(transform, false);

        imposterCamera = cameraObject.AddComponent<Camera>();
        imposterCamera.orthographic = true;
        imposterCamera.orthographicSize = halfSize;
        imposterCamera.aspect = 1f;
        imposterCamera.clearFlags = CameraClearFlags.SolidColor;
        imposterCamera.backgroundColor = Color.clear;
        imposterCamera.cullingMask = layerMask;
        imposterCamera.targetTexture = renderTexture;
        imposterCamera.allowHDR = false;
        imposterCamera.allowMSAA = false;
        imposterCamera.useOcclusionCulling = false;
        imposterCamera.nearClipPlane = 0.01f;
        imposterCamera.farClipPlane = halfSize * 2f + 2f;
        imposterCamera.depth = -50f; // render before the main camera

        UniversalAdditionalCameraData cameraData = imposterCamera.GetUniversalAdditionalCameraData();
        cameraData.renderPostProcessing = false;
        cameraData.antialiasing = AntialiasingMode.None;
        cameraData.renderShadows = false;
        cameraData.requiresColorTexture = false;
        cameraData.requiresDepthTexture = false;
    }

    private void CreateQuad(FXSettings settings, int resolution)
    {
        quadMaterial = new Material(settings.pixelImposterShader) { name = "PixelImposter (Instance)" };
        quadMaterial.mainTexture = renderTexture;
        quadMaterial.SetVector(TexSizeId, new Vector4(resolution, resolution, 0f, 0f));
        quadMaterial.SetColor(OutlineColorId, settings.imposterOutlineColor);

        var quadObject = new GameObject("ImposterQuad");
        quad = quadObject.transform;
        quad.SetParent(transform, false);

        quadObject.AddComponent<MeshFilter>().sharedMesh = Resources.GetBuiltinResource<Mesh>("Quad.fbx");
        var quadRenderer = quadObject.AddComponent<MeshRenderer>();
        quadRenderer.sharedMaterial = quadMaterial;
        quadRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        quadRenderer.receiveShadows = false;
    }

    private void LateUpdate()
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null) return;

        // Keep the real model out of the main view (also after scene reloads)
        mainCamera.cullingMask &= ~layerMask;

        Vector3 center = GetBounds().center;
        Transform view = mainCamera.transform;
        Vector3 forward = mainCamera.orthographic ? view.forward : (center - view.position).normalized;
        Quaternion rotation = Quaternion.LookRotation(forward, view.up);

        imposterCamera.transform.SetPositionAndRotation(center - forward * (halfSize + 1f), rotation);

        // Quad sits in front of the model so no part of it sinks into the ground when the model is long
        // (rollers) or seen at an angle. Under perspective it shrinks to keep the same on-screen size.
        float scale = 1f;
        if (!mainCamera.orthographic)
        {
            float centerDistance = Vector3.Dot(center - view.position, forward);
            scale = Mathf.Max(0.1f, (centerDistance - halfSize) / Mathf.Max(0.0001f, centerDistance));
        }

        // Faces the main camera with the same up/right as the imposter camera, so pixels line up
        quad.SetPositionAndRotation(center - forward * halfSize, rotation);
        quad.localScale = Vector3.one * (halfSize * 2f * scale) / Mathf.Max(0.0001f, transform.lossyScale.x);
    }

    private void OnDestroy()
    {
        if (imposterCamera != null)
            imposterCamera.targetTexture = null;
        if (renderTexture != null)
        {
            renderTexture.Release();
            Destroy(renderTexture);
        }
        if (quadMaterial != null)
            Destroy(quadMaterial);
    }

    private Bounds GetBounds()
    {
        Bounds bounds = sources[0].bounds;
        for (int i = 1; i < sources.Length; i++)
            bounds.Encapsulate(sources[i].bounds);
        return bounds;
    }
}
