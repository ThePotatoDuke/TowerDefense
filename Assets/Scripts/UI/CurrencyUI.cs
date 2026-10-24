using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// Grass counter in the top-left corner, built from code. Copies the wave text's font so the HUD matches.
public class CurrencyUI : MonoBehaviour
{
    [SerializeField] private Sprite icon;
    [Tooltip("Text whose font, size and color the counter copies.")]
    [SerializeField] private TMP_Text styleSource;
    [SerializeField] private Vector2 margin = new Vector2(20f, -20f);
    [SerializeField] private float iconSize = 40f;

    private RectTransform counter;
    private TMP_Text countText;

    private void Awake()
    {
        Build();
    }

    private void OnEnable()
    {
        Currency.OnGrassChanged += HandleGrassChanged;
        countText.text = Currency.Grass.ToString();
    }

    private void OnDisable()
    {
        Currency.OnGrassChanged -= HandleGrassChanged;
    }

    private void HandleGrassChanged(int amount)
    {
        countText.text = amount.ToString();
        if (amount == 0) return; // reset on a new run, nothing to celebrate

        counter.DOKill(true);
        counter.DOPunchScale(Vector3.one * 0.2f, 0.2f, 6, 0.5f).SetUpdate(true).SetLink(counter.gameObject);
    }

    private void Build()
    {
        counter = new GameObject("GrassCounter", typeof(RectTransform)).GetComponent<RectTransform>();
        counter.SetParent(transform, false);
        counter.anchorMin = counter.anchorMax = new Vector2(0f, 1f);
        counter.pivot = new Vector2(0f, 1f);
        counter.anchoredPosition = margin;
        counter.sizeDelta = new Vector2(200f, iconSize);

        var iconImage = new GameObject("Icon", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
        var iconRect = iconImage.rectTransform;
        iconRect.SetParent(counter, false);
        iconRect.anchorMin = iconRect.anchorMax = iconRect.pivot = new Vector2(0f, 0.5f);
        iconRect.anchoredPosition = Vector2.zero;
        iconRect.sizeDelta = Vector2.one * iconSize;
        iconImage.sprite = icon;
        iconImage.preserveAspect = true;
        iconImage.raycastTarget = false;

        countText = new GameObject("Count", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
        var textRect = countText.rectTransform;
        textRect.SetParent(counter, false);
        textRect.anchorMin = textRect.anchorMax = textRect.pivot = new Vector2(0f, 0.5f);
        textRect.anchoredPosition = new Vector2(iconSize + 8f, 0f);
        textRect.sizeDelta = new Vector2(150f, iconSize);
        countText.alignment = TextAlignmentOptions.MidlineLeft;
        countText.raycastTarget = false;
        countText.text = "0";

        if (styleSource != null)
        {
            countText.font = styleSource.font;
            countText.fontSize = styleSource.fontSize * 0.9f;
            countText.color = styleSource.color;
        }
    }
}
