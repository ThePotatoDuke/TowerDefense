using DG.Tweening;
using UnityEngine;

// Gives a sprite its own SpriteFX material and exposes the shader's effects
// (hit flash, tint, dissolve, shine, emission) to gameplay code.
public class SpriteFX : MonoBehaviour
{
    [Tooltip("Sprite to drive. Defaults to the first SpriteRenderer in children.")]
    [SerializeField] private SpriteRenderer target;

    private static readonly int FlashColorId = Shader.PropertyToID("_FlashColor");
    private static readonly int FlashAmountId = Shader.PropertyToID("_FlashAmount");
    private static readonly int TintColorId = Shader.PropertyToID("_TintColor");
    private static readonly int TintAmountId = Shader.PropertyToID("_TintAmount");
    private static readonly int DissolveId = Shader.PropertyToID("_Dissolve");
    private static readonly int DissolveEdgeColorId = Shader.PropertyToID("_DissolveEdgeColor");
    private static readonly int DissolveEdgeWidthId = Shader.PropertyToID("_DissolveEdgeWidth");
    private static readonly int ShineColorId = Shader.PropertyToID("_ShineColor");
    private static readonly int ShineStrengthId = Shader.PropertyToID("_ShineStrength");
    private static readonly int ShineSpeedId = Shader.PropertyToID("_ShineSpeed");
    private static readonly int ShineSpacingId = Shader.PropertyToID("_ShineSpacing");
    private static readonly int ShineWidthId = Shader.PropertyToID("_ShineWidth");
    private static readonly int ShineOffsetId = Shader.PropertyToID("_ShineOffset");
    private static readonly int EmissionId = Shader.PropertyToID("_Emission");
    private static readonly int TexSizeId = Shader.PropertyToID("_TexSize");

    private Material material;
    private Texture lastTexture;
    private Tween flashTween;
    private Tween dissolveTween;

    public bool IsReady => material != null;

    private void Awake()
    {
        if (target == null)
            target = GetComponentInChildren<SpriteRenderer>();

        FXSettings settings = FXSettings.Instance;
        if (target == null || settings == null || settings.spriteFXShader == null)
        {
            enabled = false;
            return;
        }

        material = new Material(settings.spriteFXShader) { name = "SpriteFX (Instance)" };
        material.SetColor(FlashColorId, settings.flashColor);
        material.SetColor(DissolveEdgeColorId, settings.dissolveEdgeColor);
        material.SetFloat(DissolveEdgeWidthId, settings.dissolveEdgeWidth);
        material.SetColor(ShineColorId, settings.shineColor);
        material.SetFloat(ShineSpeedId, settings.shineSpeed);
        material.SetFloat(ShineSpacingId, settings.shineSpacing);
        material.SetFloat(ShineWidthId, settings.shineWidth);
        material.SetFloat(ShineOffsetId, Random.Range(0f, settings.shineSpacing)); // desync shines

        target.sharedMaterial = material;
        UpdateTextureSize();
    }

    private void LateUpdate()
    {
        // Animators can swap to a sprite on another texture
        UpdateTextureSize();
    }

    private void OnDestroy()
    {
        flashTween?.Kill();
        dissolveTween?.Kill();
        if (material != null)
            Destroy(material);
    }

    private void UpdateTextureSize()
    {
        if (target.sprite == null) return;

        Texture texture = target.sprite.texture;
        if (texture == lastTexture) return;

        lastTexture = texture;
        material.SetVector(TexSizeId, new Vector4(texture.width, texture.height, 0f, 0f));
    }

    public void Flash(float duration = -1f)
    {
        if (!IsReady) return;
        if (duration < 0f) duration = FXSettings.Instance.flashDuration;

        flashTween?.Kill();
        material.SetFloat(FlashAmountId, 1f);
        flashTween = material.DOFloat(0f, FlashAmountId, duration)
                             .SetEase(Ease.InQuad)
                             .SetLink(gameObject);
    }

    // Returns how long the dissolve takes, so the caller can destroy the object afterwards
    public float Dissolve()
    {
        if (!IsReady) return 0f;

        float duration = FXSettings.Instance.dissolveDuration;
        dissolveTween?.Kill();
        material.SetFloat(DissolveId, 0f);
        dissolveTween = material.DOFloat(1f, DissolveId, duration)
                                .SetEase(Ease.InQuad)
                                .SetLink(gameObject);
        return duration;
    }

    public void SetTint(Color color, float amount)
    {
        if (!IsReady) return;
        material.SetColor(TintColorId, color);
        material.SetFloat(TintAmountId, amount);
    }

    public void SetShine(bool on)
    {
        if (!IsReady) return;
        material.SetFloat(ShineStrengthId, on ? 1f : 0f);
    }

    public void SetEmission(float emission)
    {
        if (!IsReady) return;
        material.SetFloat(EmissionId, emission);
    }
}
