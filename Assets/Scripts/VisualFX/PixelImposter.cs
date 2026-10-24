using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// Renders a 3D model as outlined pixel art so it matches the sprites.
// A private camera at the main camera's position, zoomed in on the model, renders it into a tiny
// RenderTexture every frame, so the pixel art has exactly the scene's perspective even for long models
// like rollers. A camera-facing quad shows that texture with point filtering and an outline, writing
// the model's real depth per pixel (stored in the texture's alpha), so it sorts against the ground
// and other objects like the model would. The model itself moves to an unused layer that the main
// camera no longer draws.
public class PixelImposter : MonoBehaviour
{
    private static readonly int TexSizeId = Shader.PropertyToID("_TexSize");
    private static readonly int OutlineColorId = Shader.PropertyToID("_OutlineColor");
    private static readonly int NearId = Shader.PropertyToID("_ImposterNear");
    private static readonly int FarId = Shader.PropertyToID("_ImposterFar");
    private static readonly int WriteDepthId = Shader.PropertyToID("_WriteDepth");
    private static readonly List<PixelImposter> activeImposters = new();

    private Renderer[] sources;
    private readonly List<Material> sourceMaterials = new();
    private Camera imposterCamera;
    private RenderTexture renderTexture;
    private Material quadMaterial;
    private Transform quad;
    private float halfSize;
    private int layerMask;
    private float imposterNear;
    private float imposterFar = 1f;

    private void Awake()
    {
        FXSettings settings = FXSettings.Instance;
        sources = GetComponentsInChildren<MeshRenderer>();
        if (sources.Length == 0 || settings == null || settings.pixelImposterShader == null || settings.imposterSourceShader == null)
        {
            enabled = false;
            return;
        }

        int layer = settings.pixelArtLayer;
        layerMask = 1 << layer;
        foreach (Renderer source in sources)
        {
            source.gameObject.layer = layer;
            UseSourceMaterials(source, settings.imposterSourceShader);
        }

        // Square frame big enough for the model at any rotation, plus room for the outline
        Bounds bounds = GetBounds();
        float maxExtent = Mathf.Max(bounds.extents.x, Mathf.Max(bounds.extents.y, bounds.extents.z));
        halfSize = Mathf.Max(maxExtent * 1.25f, bounds.extents.magnitude * 1.05f);
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

    // Swap in materials that store distance from the camera in alpha, keeping each one's texture and color
    private void UseSourceMaterials(Renderer source, Shader shader)
    {
        Material[] originals = source.sharedMaterials;
        var replacements = new Material[originals.Length];
        for (int i = 0; i < originals.Length; i++)
        {
            Material original = originals[i];
            var replacement = new Material(shader) { name = $"{(original != null ? original.name : "Model")} (Imposter)" };
            if (original != null)
            {
                string textureName = original.HasProperty("_BaseMap") ? "_BaseMap" : "_MainTex";
                if (original.HasProperty(textureName))
                {
                    replacement.mainTexture = original.GetTexture(textureName);
                    replacement.mainTextureScale = original.GetTextureScale(textureName);
                    replacement.mainTextureOffset = original.GetTextureOffset(textureName);
                }
                if (original.HasProperty("_BaseColor"))
                    replacement.color = original.GetColor("_BaseColor");
                else if (original.HasProperty("_Color"))
                    replacement.color = original.GetColor("_Color");
            }
            replacements[i] = replacement;
            sourceMaterials.Add(replacement);
        }
        source.sharedMaterials = replacements;
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
        float parentScale = Mathf.Max(0.0001f, transform.lossyScale.x);

        if (mainCamera.orthographic)
        {
            // Flat view: a parallel render through the model's center, sorted at the quad's own depth
            Quaternion flatRotation = view.rotation;
            imposterCamera.orthographic = true;
            imposterCamera.orthographicSize = halfSize;
            imposterCamera.nearClipPlane = 0.01f;
            imposterCamera.farClipPlane = halfSize * 2f + 2f;
            imposterCamera.transform.SetPositionAndRotation(center - view.forward * (halfSize + 1f), flatRotation);
            quadMaterial.SetFloat(WriteDepthId, 0f);

            quad.SetPositionAndRotation(center, flatRotation);
            quad.localScale = Vector3.one * (halfSize * 2f) / parentScale;
            return;
        }

        Vector3 toCenter = center - view.position;
        float distance = Mathf.Max(0.01f, toCenter.magnitude);
        Vector3 forward = toCenter / distance;
        Quaternion rotation = Quaternion.LookRotation(forward, view.up);
        float halfAngle = Mathf.Asin(Mathf.Clamp(halfSize / distance, 0.01f, 0.95f));

        // Range of distances the model can be at; the model's material encodes where in it each pixel lies
        float near = Mathf.Max(0.01f, distance - halfSize);
        float far = distance + halfSize;

        imposterCamera.orthographic = false;
        imposterCamera.fieldOfView = halfAngle * 2f * Mathf.Rad2Deg;
        imposterCamera.nearClipPlane = near * 0.9f;
        imposterCamera.farClipPlane = far + 0.5f;
        imposterCamera.transform.SetPositionAndRotation(view.position, rotation);

        imposterNear = near;
        imposterFar = far;
        quadMaterial.SetFloat(NearId, near);
        quadMaterial.SetFloat(FarId, far);
        quadMaterial.SetFloat(WriteDepthId, 1f);

        // The quad sits just in front of the model, sized to cover exactly what the imposter camera sees.
        // Its own position doesn't affect sorting, since every pixel writes the model's real depth.
        quad.SetPositionAndRotation(view.position + forward * near, rotation);
        quad.localScale = Vector3.one * (2f * near * Mathf.Tan(halfAngle)) / parentScale;
    }

    private void OnEnable()
    {
        if (activeImposters.Count == 0)
            RenderPipelineManager.beginCameraRendering += HandleBeginCameraRendering;
        activeImposters.Add(this);
    }

    private void OnDisable()
    {
        activeImposters.Remove(this);
        SetSourcesVisible(true);
        if (activeImposters.Count == 0)
            RenderPipelineManager.beginCameraRendering -= HandleBeginCameraRendering;
    }

    // Every imposter camera sees the whole pixel-art layer, so nearby models would bleed into
    // each other's frames and get cut off at the edges. Each imposter camera only draws its own model.
    private static void HandleBeginCameraRendering(ScriptableRenderContext context, Camera camera)
    {
        PixelImposter owner = null;
        foreach (PixelImposter imposter in activeImposters)
        {
            if (imposter.imposterCamera == camera)
            {
                owner = imposter;
                break;
            }
        }

        // Other cameras (main, scene view) see every model as usual
        foreach (PixelImposter imposter in activeImposters)
            imposter.SetSourcesVisible(owner == null || imposter == owner);

        // Distance range the model's material encodes into alpha. Global rather than per material,
        // so material copies (e.g. made by the hit flash) pick it up too.
        if (owner != null)
        {
            Shader.SetGlobalFloat(NearId, owner.imposterNear);
            Shader.SetGlobalFloat(FarId, owner.imposterFar);
        }
    }

    private void SetSourcesVisible(bool visible)
    {
        if (sources == null) return;
        foreach (Renderer source in sources)
        {
            if (source != null)
                source.forceRenderingOff = !visible;
        }
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
        foreach (Material material in sourceMaterials)
            Destroy(material);
    }

    private Bounds GetBounds()
    {
        Bounds bounds = sources[0].bounds;
        for (int i = 1; i < sources.Length; i++)
            bounds.Encapsulate(sources[i].bounds);
        return bounds;
    }
}
