using DG.Tweening;
using TMPro;
using UnityEngine;

public class WaveUI : MonoBehaviour
{
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private EndlessWaveManager waveManager;

    [Header("Wave Timer")]
    [Tooltip("Shows the time left to clear the wave (empty between waves).")]
    [SerializeField] private TMP_Text timerText;
    [Tooltip("The timer turns this colour and ticks for the last few seconds, and in overtime.")]
    [SerializeField] private Color hurryColor = new Color(1f, 0.3f, 0.25f);
    [SerializeField] private float hurrySeconds = 10f;
    [SerializeField] private string overtimeText = "OVERTIME";

    [Header("Wave Cleared")]
    [Tooltip("The counter flashes this color and punches when a wave is cleared.")]
    [SerializeField] private Color clearedColor = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private float clearedPunch = 0.35f;
    [SerializeField] private float clearedDuration = 0.6f;
    [Tooltip("Smaller pop when the next wave's number comes in.")]
    [SerializeField] private float newWavePunch = 0.15f;

    private Color baseColor;
    private Color timerColor;
    private int shownSecond = -1;
    private string digitWidth; // every digit as wide as the widest, so the time doesn't shuffle sideways

    private void Awake()
    {
        if (waveText != null)
            baseColor = waveText.color;
        if (timerText != null)
        {
            timerColor = timerText.color;
            timerText.text = "";
            digitWidth = WidestDigit(timerText.font);
        }
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

        if (!waveManager.TimerRunning)
        {
            timerText.text = "";
            shownSecond = -1;
            return;
        }

        if (waveManager.InOvertime)
        {
            timerText.text = overtimeText;
            // A slow red pulse
            timerText.color = Color.Lerp(timerColor, hurryColor, 0.5f + 0.5f * Mathf.Sin(Time.time * 8f));
            return;
        }

        int seconds = Mathf.CeilToInt(waveManager.TimeLeft);
        if (seconds == shownSecond) return;
        shownSecond = seconds;
        timerText.text = $"{Digits((seconds / 60).ToString())}:{Digits((seconds % 60).ToString("00"))}";

        // The last few seconds tick in red
        bool hurry = seconds <= hurrySeconds;
        timerText.color = hurry ? hurryColor : timerColor;
        if (hurry)
        {
            RectTransform rect = timerText.rectTransform;
            rect.DOKill(true);
            rect.DOPunchScale(Vector3.one * 0.2f, 0.25f, 4, 0.5f).SetLink(rect.gameObject);
        }
    }

    private void OnEnable()
    {
        if (waveManager != null)
            waveManager.OnWaveChanged += SetWave;
        GameEvents.OnWaveCleared += HandleWaveCleared;
    }

    private void OnDisable()
    {
        if (waveManager != null)
            waveManager.OnWaveChanged -= SetWave;
        GameEvents.OnWaveCleared -= HandleWaveCleared;
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

    private void Punch(float amount, float duration)
    {
        RectTransform rect = waveText.rectTransform;
        rect.DOKill(true);
        rect.DOPunchScale(Vector3.one * amount, duration, 6, 0.5f).SetUpdate(true).SetLink(rect.gameObject);
    }
}
