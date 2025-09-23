using TMPro;
using UnityEngine;

public class WaveUI : MonoBehaviour
{
    [SerializeField] private TMP_Text waveText;
    [SerializeField] private EndlessWaveManager waveManager;

    private void OnEnable()
    {
        if (waveManager != null)
            waveManager.OnWaveChanged += SetWave;
    }

    private void OnDisable()
    {
        if (waveManager != null)
            waveManager.OnWaveChanged -= SetWave;
    }

    private void SetWave(int waveNumber)
    {
        if (waveText != null)
            waveText.text = $"Wave {waveNumber}";
    }
}
