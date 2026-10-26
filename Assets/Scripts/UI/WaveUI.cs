using DG.Tweening;
using TMPro;
using UnityEngine;

public class WaveUI : MonoBehaviour
{
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private EndlessWaveManager waveManager;

    [Header("Wave Cleared")]
    [Tooltip("The counter flashes this color and punches when a wave is cleared.")]
    [SerializeField] private Color clearedColor = new Color(1f, 0.85f, 0.3f);
    [SerializeField] private float clearedPunch = 0.35f;
    [SerializeField] private float clearedDuration = 0.6f;
    [Tooltip("Smaller pop when the next wave's number comes in.")]
    [SerializeField] private float newWavePunch = 0.15f;

    private Color baseColor;

    private void Awake()
    {
        if (waveText != null)
            baseColor = waveText.color;
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
