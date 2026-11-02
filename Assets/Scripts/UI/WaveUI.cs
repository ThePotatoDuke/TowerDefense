using DG.Tweening;
using TMPro;
using UnityEngine;

public class WaveUI : MonoBehaviour
{
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private EndlessWaveManager waveManager;

    [Header("Harvest Bonus")]
    [Tooltip("Shows the share of this wave's grass the harvest bonus is worth now, shrinking as the wave " +
             "goes on (empty between waves and once it's gone).")]
    [SerializeField] private TMP_Text timerText;
    [Tooltip("It turns this colour and ticks for the last few seconds of the bonus.")]
    [SerializeField] private Color hurryColor = new Color(1f, 0.3f, 0.25f);
    [SerializeField] private float hurrySeconds = 10f;
    [Tooltip("Where the bonus grass flies to when it's earned; the scene's grass counter if empty.")]
    [SerializeField] private CurrencyUI grassCounter;
    [Tooltip("The bonus turns this colour before it flies off.")]
    [SerializeField] private Color bonusLightColor = new Color(1f, 1f, 0.85f);
    [Tooltip("Seconds the bonus grass shows before it lights up and flies to the counter.")]
    [SerializeField] private float bonusShowTime = 0.35f;
    [SerializeField] private float bonusLightTime = 0.15f;
    [SerializeField] private float bonusFlyTime = 0.45f;

    [Header("Wave Cleared")]
    [Tooltip("The counter flashes this color and punches when a wave is cleared.")]
    [SerializeField] private Color clearedColor = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private float clearedPunch = 0.35f;
    [SerializeField] private float clearedDuration = 0.6f;
    [Tooltip("Smaller pop when the next wave's number comes in.")]
    [SerializeField] private float newWavePunch = 0.15f;

    private Color baseColor;
    private Color timerColor;
    private int shownPercent = -1;
    private float bonusShownUntil = -1f;
    private string digitWidth; // every digit as wide as the widest, so it doesn't shuffle sideways

    private void Awake()
    {
        if (waveText != null)
            baseColor = waveText.color;
        if (timerText != null)
        {
            timerColor = timerText.color;
            timerText.text = "";
            // Centred in its box, so it stays in the middle whatever it says
            timerText.horizontalAlignment = HorizontalAlignmentOptions.Center;
            timerText.verticalAlignment = VerticalAlignmentOptions.Middle;
            digitWidth = WidestDigit(timerText.font);
        }
    }

    private void OnEnable()
    {
        if (waveManager != null)
        {
            waveManager.OnWaveChanged += SetWave;
            waveManager.OnHarvestBonus += ShowHarvestBonus;
        }
        GameEvents.OnWaveCleared += HandleWaveCleared;
    }

    private void OnDisable()
    {
        if (waveManager != null)
        {
            waveManager.OnWaveChanged -= SetWave;
            waveManager.OnHarvestBonus -= ShowHarvestBonus;
        }
        GameEvents.OnWaveCleared -= HandleWaveCleared;
    }

    // The widest digit's width as a fraction of the font size, for TextMeshPro's <mspace> tag
    private static string WidestDigit(TMP_FontAsset font)
    {
        if (font == null || font.faceInfo.pointSize <= 0) return null;

        float widest = 0f;
        for (char digit = '0'; digit <= '9'; digit++)
        {
            // Dynamic fonts add the glyph if it isn't in the atlas yet
            if (font.HasCharacter(digit, false, true) && font.characterLookupTable.TryGetValue(digit, out TMP_Character character))
                widest = Mathf.Max(widest, character.glyph.metrics.horizontalAdvance * character.scale);
        }
        if (widest <= 0f) return null;
        return (widest / font.faceInfo.pointSize).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "em";
    }

    private string Digits(string digits) => digitWidth != null ? $"<mspace={digitWidth}>{digits}</mspace>" : digits;

    private void Update()
    {
        if (timerText == null || waveManager == null) return;
        if (Time.unscaledTime < bonusShownUntil) return; // the grass just earned is showing

        int percent = Mathf.CeilToInt(waveManager.BonusPercentNow);
        if (!waveManager.TimerRunning || percent <= 0)
        {
            timerText.text = "";
            shownPercent = -1;
            return;
        }
        if (percent == shownPercent) return;
        shownPercent = percent;
        timerText.text = $"+{Digits(percent.ToString())}%";

        // The last few seconds tick down in red
        bool hurry = waveManager.TimeLeft <= hurrySeconds;
        timerText.color = hurry ? hurryColor : timerColor;
        if (hurry)
            Punch(timerText.rectTransform, 0.2f, 0.25f);
    }

    // Cleared in time: the bonus grass pops up in the cleared colour, lights up and flies into the grass
    // counter, which goes up as it lands (well before the shop can open). A copy flies, so the bonus text
    // stays where it's laid out.
    private void ShowHarvestBonus(int grass)
    {
        if (grassCounter == null)
            grassCounter = FindAnyObjectByType<CurrencyUI>();
        if (timerText == null || grassCounter == null)
        {
            Currency.AddGrass(grass);
            return;
        }

        TMP_Text flying = Instantiate(timerText, timerText.transform.parent);
        flying.name = "HarvestBonus";
        flying.text = $"+{grass}";
        flying.color = clearedColor;
        timerText.text = "";
        shownPercent = -1;
        bonusShownUntil = Time.unscaledTime + bonusShowTime + bonusLightTime + bonusFlyTime;

        Transform target = grassCounter.Counter;
        bool paid = false;
        void Pay()
        {
            if (paid) return;
            paid = true;
            Currency.AddGrass(grass);
        }

        RectTransform rect = flying.rectTransform;
        rect.localScale = Vector3.one;
        DOTween.Sequence().SetUpdate(true).SetLink(flying.gameObject)
            .Append(rect.DOPunchScale(Vector3.one * clearedPunch, bonusShowTime, 6, 0.5f))
            .Append(flying.DOColor(bonusLightColor, bonusLightTime))
            .Append(rect.DOMove(target.position, bonusFlyTime).SetEase(Ease.InQuad))
            .Join(rect.DOScale(0.5f, bonusFlyTime).SetEase(Ease.InQuad))
            .OnComplete(() =>
            {
                Pay();
                Destroy(flying.gameObject);
            })
            .OnKill(Pay); // never lost, even if the HUD goes away mid-flight
    }

    private void SetWave(int waveNumber)
    {
        if (waveText == null) return;

        waveText.text = $"Wave {waveNumber}";
        if (waveNumber > 1)
            Punch(newWavePunch, 0.25f);
    }

    // A bounce, a wiggle and a flash of color that fades back
    private void HandleWaveCleared()
    {
        if (waveText == null) return;

        Punch(clearedPunch, clearedDuration);
        waveText.rectTransform.DOPunchRotation(new Vector3(0f, 0f, 8f), clearedDuration, 8, 0.5f)
                .SetUpdate(true).SetLink(waveText.gameObject);

        waveText.DOKill();
        waveText.color = clearedColor;
        waveText.DOColor(baseColor, clearedDuration).SetEase(Ease.InQuad).SetUpdate(true).SetLink(waveText.gameObject);
    }

    private void Punch(float amount, float duration) => Punch(waveText.rectTransform, amount, duration);

    private static void Punch(RectTransform rect, float amount, float duration)
    {
        rect.DOKill(true);
        rect.DOPunchScale(Vector3.one * amount, duration, 6, 0.5f).SetUpdate(true).SetLink(rect.gameObject);
    }
}
