using TMPro;
using UnityEngine;

// Fills in the wave the run reached when the game-over panel is shown. The panel itself (board, wavy
// title, wave line, buttons) is laid out in the scene; Tools > Create UI Layout Objects builds a starting one.
public class GameOverScreen : MonoBehaviour
{
    [SerializeField] private TMP_Text waveText;
    [Tooltip("{0} is replaced with the wave number.")]
    [SerializeField] private string waveFormat = "Wave reached: {0}";

    private void OnEnable()
    {
        if (waveText == null) return;

        var waveManager = FindAnyObjectByType<EndlessWaveManager>();
        int wave = waveManager != null ? waveManager.CurrentWave : 0;
        waveText.text = string.Format(waveFormat, wave);
    }
}
