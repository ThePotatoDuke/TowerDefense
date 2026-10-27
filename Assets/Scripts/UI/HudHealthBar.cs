using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// The player's or the castle's health in the HUD (top right): a bar that fills in whole art pixels, with
// a pale trail that catches up a moment after a hit so the loss shows, a little knock when hurt, and a
// red pulse when low. Laid out by Tools > Create HUD Health Bars, which also switches off the bars that
// float over the player and the castle.
public class HudHealthBar : MonoBehaviour
{
    public enum Owner { Player, Castle }

    [SerializeField] private Owner owner;
    [Tooltip("Stretched across the inside of the board; the fill and trail sit in it.")]
    [SerializeField] private RectTransform inner;
    [SerializeField] private Image fill;
    [Tooltip("Behind the fill: shows what was just lost, then shrinks to match.")]
    [SerializeField] private Image trail;
    [Tooltip("Optional: \"18/20\".")]
    [SerializeField] private TMP_Text amount;

    [SerializeField] private float trailDelay = 0.35f;
    [SerializeField] private float trailTime = 0.4f;
    [Tooltip("Below this share of health the fill pulses this colour.")]
    [SerializeField, Range(0f, 1f)] private float lowHealth = 0.3f;
    [SerializeField] private Color lowColor = new Color(1f, 0.3f, 0.25f);

    private IHasHealth target;
    private Color fillColor;
    private float shown = 1f;
    private float trailShown = 1f;
    private Tween trailTween;

    private void Start()
    {
        target = owner == Owner.Player ? Player.Instance : FindAnyObjectByType<CastleHealth>();
        if (fill != null)
            fillColor = fill.color;
        if (target == null) return;

        target.OnHealthChanged += HandleHealthChanged;
        shown = trailShown = Ratio();
        Draw();
    }

    private void OnDestroy()
    {
        if (target != null)
            target.OnHealthChanged -= HandleHealthChanged;
    }

    private void Update()
    {
        if (fill == null) return;
        bool low = shown > 0f && shown <= lowHealth;
        fill.color = low ? Color.Lerp(fillColor, lowColor, 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * 10f)) : fillColor;
    }

    private float Ratio() => target.MaxHealth > 0f ? Mathf.Clamp01(target.CurrentHealth / target.MaxHealth) : 0f;

    private void HandleHealthChanged(float ratio)
    {
        bool hurt = ratio < shown;
        shown = ratio;

        trailTween?.Kill();
        if (hurt)
        {
            // The trail holds where it was, then drains down to the new health
            trailTween = DOTween.To(() => trailShown, value => { trailShown = value; Draw(); }, ratio, trailTime)
                .SetDelay(trailDelay).SetEase(Ease.InQuad).SetUpdate(true).SetLink(gameObject);
            var rect = (RectTransform)transform;
            rect.DOKill(true);
            rect.DOPunchAnchorPos(new Vector2(-6f, 0f), 0.25f, 8, 0.5f).SetUpdate(true).SetLink(gameObject);
        }
        else
            trailShown = ratio; // healing: no trail

        Draw();
    }

    private void Draw()
    {
        SetWidth(fill, shown);
        SetWidth(trail, Mathf.Max(trailShown, shown));
        if (amount != null && target != null)
            amount.text = $"{Mathf.CeilToInt(target.CurrentHealth)}/{Mathf.CeilToInt(target.MaxHealth)}";
    }

    // Filled from the left, cut to whole art pixels so the end stays crisp
    private void SetWidth(Image image, float ratio)
    {
        if (image == null || inner == null) return;
        float pixels = Mathf.Max(1f, inner.rect.width / UIPixels.Scale);
        float snapped = Mathf.Round(ratio * pixels) / pixels;
        RectTransform rect = image.rectTransform;
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = new Vector2(snapped, 1f);
        rect.offsetMin = rect.offsetMax = Vector2.zero;
    }
}
