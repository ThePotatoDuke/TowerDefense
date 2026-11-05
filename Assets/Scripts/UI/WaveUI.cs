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

    [Header("Boss Banner")]
    [Tooltip("A boss wave's name slams into the middle of the screen in this colour, in the wave text's font.")]
    [SerializeField] private Color bannerColor = new Color(1f, 0.35f, 0.2f);
    [SerializeField] private Color bannerSubtitleColor = new Color(1f, 0.85f, 0.3f);
    [Tooltip("The banner's title, as a multiple of the wave text's size; the line under it is half that.")]
    [SerializeField] private float bannerSize = 1.6f;
    [Tooltip("How far above the middle of the screen it sits, in canvas units.")]
    [SerializeField] private float bannerHeight = 80f;

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
        GameEvents.OnBossWaveStarted += ShowBanner;
    }

    private void OnDisable()
    {
        if (waveManager != null)
        {
            waveManager.OnWaveChanged -= SetWave;
            waveManager.OnHarvestBonus -= ShowHarvestBonus;
        }
        GameEvents.OnWaveCleared -= HandleWaveCleared;
        GameEvents.OnBossWaveStarted -= ShowBanner;
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
        {
            Punch(newWavePunch, 0.25f);
            if (waveManager == null || !waveManager.IsBossWave) // a boss wave has its banner's sound
                Sounds.Play(Sound.NextWave);
        }
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

    // PvZ style: the boss wave's name slams in big in the middle of the screen with its subtitle under it,
    // shakes, wobbles, and swells away as the wave's enemies start coming
    private void ShowBanner(BossWave wave)
    {
        Canvas canvas = waveText != null ? waveText.canvas : null;
        if (canvas == null) return;
        Sounds.Play(Sound.SpecialWaveBanner);
        float total = WaveSettings.Instance != null ? WaveSettings.Instance.bossBannerTime : 2.5f;

        RectTransform banner = new GameObject("BossBanner", typeof(RectTransform), typeof(CanvasGroup)).GetComponent<RectTransform>();
        banner.SetParent(canvas.rootCanvas.transform, false);
        banner.anchorMin = banner.anchorMax = banner.pivot = new Vector2(0.5f, 0.5f);
        banner.anchoredPosition = new Vector2(0f, bannerHeight);
        banner.sizeDelta = new Vector2(1800f, 300f);
        var group = banner.GetComponent<CanvasGroup>();
        group.blocksRaycasts = false;
        group.alpha = 0f;

        float size = waveText.fontSize * bannerSize;
        BannerLine(banner, wave.bannerTitle, size, bannerColor, size * 0.35f);
        if (!string.IsNullOrEmpty(wave.bannerSubtitle))
            BannerLine(banner, wave.bannerSubtitle, size * 0.5f, bannerSubtitleColor, -size * 0.55f);

        float slam = 0.25f, fade = 0.35f;
        float hold = Mathf.Max(0.2f, total - slam - fade);
        banner.localScale = Vector3.one * 3f;
        DOTween.Sequence().SetUpdate(true).SetLink(banner.gameObject)
            .Append(banner.DOScale(1f, slam).SetEase(Ease.InQuad))
            .Join(DOTween.To(() => group.alpha, a => group.alpha = a, 1f, slam * 0.6f))
            .Append(banner.DOShakeAnchorPos(0.3f, 18f, 30, 90f, true))
            .Join(banner.DOPunchRotation(new Vector3(0f, 0f, 6f), hold, 4, 0.5f))
            .Insert(slam + hold, banner.DOScale(1.3f, fade).SetEase(Ease.InQuad))
            .Join(DOTween.To(() => group.alpha, a => group.alpha = a, 0f, fade))
            .OnComplete(() => Destroy(banner.gameObject));
    }

    // A line of the banner: a copy of the wave text (its font and outline), centred, at this size and height
    private void BannerLine(RectTransform banner, string content, float size, Color color, float y)
    {
        TMP_Text line = Instantiate(waveText, banner);
        line.name = "Line";
        line.text = content;
        line.fontSize = size;
        line.color = color;
        line.horizontalAlignment = HorizontalAlignmentOptions.Center;
        line.verticalAlignment = VerticalAlignmentOptions.Middle;
        line.textWrappingMode = TextWrappingModes.NoWrap;
        RectTransform rect = line.rectTransform;
        rect.DOKill();
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = new Vector2(0f, y);
        rect.sizeDelta = new Vector2(banner.sizeDelta.x, size * 1.4f);
        rect.localScale = Vector3.one;
        rect.localRotation = Quaternion.identity;
    }

    private void Punch(float amount, float duration) => Punch(waveText.rectTransform, amount, duration);

    private static void Punch(RectTransform rect, float amount, float duration)
    {
        rect.DOKill(true);
        rect.DOPunchScale(Vector3.one * amount, duration, 6, 0.5f).SetUpdate(true).SetLink(rect.gameObject);
    }
}
