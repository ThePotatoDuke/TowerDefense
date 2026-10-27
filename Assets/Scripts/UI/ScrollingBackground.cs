using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// The main menu's and the shop's background: a checkerboard of pixel diamonds filling its rect that drifts
// forever, drawn by the PixelDiamonds shader from the size and colours set here (changes show straight away
// in the editor), at a whole-number pixel scale so it stays crisp, and still scrolling while the game is
// paused. Reveal grows the diamonds in one by one (or shrinks them away), starting from the corner they
// drift away from.
[ExecuteAlways]
[RequireComponent(typeof(RawImage))]
public class ScrollingBackground : MonoBehaviour
{
    private static readonly int DiamondColorId = Shader.PropertyToID("_DiamondColor");
    private static readonly int GapColorId = Shader.PropertyToID("_GapColor");
    private static readonly int PixelsId = Shader.PropertyToID("_Pixels");
    private static readonly int UVRectId = Shader.PropertyToID("_UVRect");
    private static readonly int RevealDirectionId = Shader.PropertyToID("_RevealDirection");
    private static readonly int RevealId = Shader.PropertyToID("_Reveal");
    private static readonly int RevealSpreadId = Shader.PropertyToID("_RevealSpread");

    [Header("Diamonds")]
    [Tooltip("Width of one diamond, in art pixels.")]
    [SerializeField, Range(4, 128)] private int diamondSize = 32;
    [Tooltip("Screen pixels per art pixel at 1080p (the rest of the UI uses 4).")]
    [SerializeField, Range(1, 10)] private int pixelScale = 4;
    [SerializeField] private Color diamondColor = new Color32(47, 87, 83, 255);
    [SerializeField] private Color gapColor = new Color32(40, 53, 64, 255);

    [Header("Scrolling")]
    [Tooltip("Art pixels per second, sideways and up. The diamonds also grow in along this.")]
    [SerializeField] private Vector2 speed = new(6f, 3f);

    [Header("Growing In")]
    [Tooltip("How many diamonds grow at once while revealing (low = one after another, 1 = nearly all together).")]
    [SerializeField, Range(0.05f, 1f)] private float revealSpread = 0.3f;

    private RawImage image;
    private RectTransform rect;
    private Material material;
    private Vector2 offset; // in art pixels
    private float reveal = 1f;

    // 0 = no diamonds, 1 = all full size
    public float Reveal
    {
        get => reveal;
        set
        {
            reveal = Mathf.Clamp01(value);
            Apply();
        }
    }

    // Grows the diamonds in (or shrinks them away) over the given seconds, also while paused
    public Tween PlayReveal(bool show, float duration)
    {
        DOTween.Kill(this);
        return DOTween.To(() => Reveal, value => Reveal = value, show ? 1f : 0f, duration)
            .SetEase(Ease.Linear).SetUpdate(true).SetTarget(this).SetLink(gameObject);
    }

    private void OnEnable()
    {
        image = GetComponent<RawImage>();
        rect = (RectTransform)transform;

        if (material == null)
        {
            var shader = Resources.Load<Shader>("PixelDiamonds");
            if (shader == null)
            {
                Debug.LogWarning("ScrollingBackground: the PixelDiamonds shader isn't in Resources.");
                return;
            }
            material = new Material(shader) { hideFlags = HideFlags.DontSave, name = "Pixel Diamonds" };
        }
        image.texture = null;
        image.material = material;
        Apply();
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
        if (material == null) return;
        if (Application.isPlaying)
            Destroy(material);
        else
            DestroyImmediate(material);
    }

    private void Update()
    {
        if (Application.isPlaying)
            offset = new Vector2((offset.x + speed.x * Time.unscaledDeltaTime) % diamondSize,
                                 (offset.y + speed.y * Time.unscaledDeltaTime) % diamondSize);
        Apply();
    }

    private void Apply()
    {
        if (material == null || rect == null) return;

        // How many tiles fit across the rect at the pixel scale; moving the uv left scrolls the pattern right
        Vector2 size = rect.rect.size / (diamondSize * pixelScale);
        var uv = new Rect(-offset.x / diamondSize, -offset.y / diamondSize, size.x, size.y);
        image.uvRect = uv;

        material.SetColor(DiamondColorId, diamondColor);
        material.SetColor(GapColorId, gapColor);
        material.SetFloat(PixelsId, diamondSize);
        material.SetVector(UVRectId, new Vector4(uv.x, uv.y, uv.width, uv.height));
        material.SetVector(RevealDirectionId, speed);
        material.SetFloat(RevealId, reveal);
        material.SetFloat(RevealSpreadId, revealSpread);
    }
}
