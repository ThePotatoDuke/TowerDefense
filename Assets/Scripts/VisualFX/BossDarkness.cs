using DG.Tweening;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

// Night falls for a boss wave (the Bat Cave), fading in and out together:
//  - the sky turns from blue to a purple night gradient,
//  - everything takes on a dim purple moonlight (a screen-wide post-processing volume),
//  - and it's pitch black but for a circle of light round the player, its edge dithered in pixel steps
//    (Resources/BossDarkness: a full-screen image on its own canvas, under the HUD so that stays readable).
// Made the first time it's needed.
public class BossDarkness : MonoBehaviour
{
    private const float FadeTime = 2f;

    // The moonlight over everything
    private const float Exposure = -0.7f;
    private static readonly Color Tint = new(0.8f, 0.62f, 1f);
    private const float Desaturate = -20f;

    // The night sky (the PixelSky material's two tones)
    private static readonly Color SkyTop = new(0.12f, 0.05f, 0.24f);
    private static readonly Color SkyBottom = new(0.34f, 0.15f, 0.46f);

    // The light round the player, as shares of the screen's height
    private const float LightRadius = 0.32f;
    private const float LightEdge = 0.1f;
    private const float PixelSize = 4f; // screen pixels at 1080p, like the sky's dither

    private static readonly int TopColorId = Shader.PropertyToID("_TopColor");
    private static readonly int BottomColorId = Shader.PropertyToID("_BottomColor");
    private static readonly int CenterId = Shader.PropertyToID("_Center");
    private static readonly int RadiusId = Shader.PropertyToID("_Radius");
    private static readonly int BandId = Shader.PropertyToID("_Band");
    private static readonly int PixelId = Shader.PropertyToID("_Pixel");
    private static readonly int ScreenSizeId = Shader.PropertyToID("_ViewSize");

    private static BossDarkness instance;
    private Volume volume;
    private Image shade;
    private Material shadeMaterial;
    private Material daySky;   // the scene's own sky material, put back afterwards
    private Material nightSky; // a copy whose colours change
    private Color dayTop, dayBottom;
    private float weight;

    public static void FadeIn()
    {
        if (instance == null)
            instance = new GameObject("BossDarkness").AddComponent<BossDarkness>();
        instance.Fade(1f);
    }

    public static void FadeOut()
    {
        if (instance != null)
            instance.Fade(0f);
    }

    private void Awake()
    {
        var profile = ScriptableObject.CreateInstance<VolumeProfile>();
        var colour = profile.Add<ColorAdjustments>();
        colour.postExposure.Override(Exposure);
        colour.colorFilter.Override(Tint);
        colour.saturation.Override(Desaturate);
        volume = gameObject.AddComponent<Volume>();
        volume.isGlobal = true;
        volume.priority = 100f; // over the scene's own look
        volume.sharedProfile = profile;
        volume.weight = 0f;

        CreateShade();

        daySky = RenderSettings.skybox;
        if (daySky != null && daySky.HasProperty(TopColorId) && daySky.HasProperty(BottomColorId))
        {
            nightSky = new Material(daySky) { name = daySky.name + " (Night)" };
            dayTop = daySky.GetColor(TopColorId);
            dayBottom = daySky.GetColor(BottomColorId);
        }
        Apply(0f);
    }

    // The dark with the hole: a full-screen image on a canvas drawn after the world and before the HUD
    private void CreateShade()
    {
        var material = Resources.Load<Material>("BossDarkness");
        if (material == null) return;
        shadeMaterial = new Material(material);

        var canvasObject = new GameObject("BossDarknessCanvas", typeof(Canvas));
        canvasObject.transform.SetParent(transform, false);
        var canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = -10; // under the HUD's canvases

        shade = new GameObject("Shade", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        shade.transform.SetParent(canvasObject.transform, false);
        var rect = shade.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = rect.offsetMax = Vector2.zero;
        shade.material = shadeMaterial;
        shade.raycastTarget = false;
    }

    private void Fade(float target)
    {
        DOTween.Kill(this);
        DOTween.To(() => weight, Apply, target, FadeTime).SetEase(Ease.InOutSine).SetTarget(this)
            .SetLink(gameObject);
    }

    private void Apply(float value)
    {
        weight = value;
        volume.weight = value;
        if (shade != null)
        {
            shade.color = new Color(1f, 1f, 1f, value);
            shade.enabled = value > 0f;
        }

        if (nightSky != null)
        {
            nightSky.SetColor(TopColorId, Color.Lerp(dayTop, SkyTop, value));
            nightSky.SetColor(BottomColorId, Color.Lerp(dayBottom, SkyBottom, value));
            RenderSettings.skybox = value > 0f ? nightSky : daySky;
        }
    }

    // The light follows the player
    private void LateUpdate()
    {
        if (shade == null || !shade.enabled) return;
        Camera view = Camera.main;
        Player player = Player.Instance;
        if (view == null) return;

        float height = Screen.height;
        Vector3 center = player != null
            ? view.WorldToScreenPoint(player.transform.position + Vector3.up * 0.5f)
            : new Vector3(Screen.width * 0.5f, height * 0.5f, 0f);
        shadeMaterial.SetVector(CenterId, new Vector4(center.x, center.y, 0f, 0f));
        shadeMaterial.SetVector(ScreenSizeId, new Vector4(Screen.width, height, 0f, 0f));
        shadeMaterial.SetFloat(RadiusId, LightRadius * height);
        shadeMaterial.SetFloat(BandId, LightEdge * height);
        shadeMaterial.SetFloat(PixelId, Mathf.Max(1f, Mathf.Round(PixelSize * height / 1080f)));
    }

    private void OnDestroy()
    {
        DOTween.Kill(this);
        if (RenderSettings.skybox == nightSky && daySky != null)
            RenderSettings.skybox = daySky;
        if (nightSky != null)
            Destroy(nightSky);
        if (shadeMaterial != null)
            Destroy(shadeMaterial);
        if (volume != null && volume.sharedProfile != null)
            Destroy(volume.sharedProfile);
        if (instance == this)
            instance = null;
    }
}
