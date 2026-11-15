using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// Esc (or P, or a gamepad's Start) pauses the game while it's being played: everything stops, the music
// with it, and the pause panel pops up (Resume, Retry, Menu). Esc again or Resume carries on. Not while the
// shop's open, a scene's changing or the run's over. Tools > Create Pause Menu makes the panel from the
// game-over one, for laying out in the scene.
public class PauseMenu : MonoBehaviour
{
    public static bool IsOpen { get; private set; }

    [SerializeField] private GameObject panel;
    [SerializeField] private Button resumeButton;
    [Tooltip("Optional: shows the wave the run's on.")]
    [SerializeField] private TMP_Text waveText;
    [Tooltip("{0} is replaced with the wave number.")]
    [SerializeField] private string waveFormat = "Wave {0}";
    [SerializeField] private float popTime = 0.25f;

    private GameObject lastSelected;

    private void Awake()
    {
        IsOpen = false;
        if (panel != null)
            panel.SetActive(false);
    }

    private void OnDestroy() => IsOpen = false;

    private bool CanPause =>
        panel != null && GameStateManager.CurrentState == GameState.Playing && !Shop.IsOpen
        && Player.Instance != null && !Player.Instance.IsDead();

    private void Update()
    {
        // The bestiary, opened from here, takes the keys while it's up (and the Esc that closes it)
        if (BestiaryPanel.IsOpen || BestiaryPanel.ClosedFrame == Time.frameCount) return;

        if (TogglePressed() && !SceneTransition.Busy)
        {
            if (IsOpen)
                Resume();
            else if (CanPause)
                Pause();
        }

        if (IsOpen)
            KeepSelection();
    }

    private static bool TogglePressed()
    {
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        return keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.pKey.wasPressedThisFrame)
               || gamepad != null && gamepad.startButton.wasPressedThisFrame;
    }

    public void Pause()
    {
        if (IsOpen) return;
        IsOpen = true;
        SetGameState(GameState.Paused);
        Sounds.Play(Sound.Pause);

        if (waveText != null && EndlessWaveManager.Instance != null)
            waveText.text = string.Format(waveFormat, EndlessWaveManager.Instance.CurrentWave);

        panel.transform.SetAsLastSibling();
        panel.SetActive(true);
        Transform board = panel.transform;
        board.DOKill(true);
        board.localScale = Vector3.zero;
        board.DOScale(1f, popTime).SetEase(Ease.OutBack).SetUpdate(true).SetLink(panel);

        UINavigation.Reset();
        lastSelected = null;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
    }

    public void Resume()
    {
        if (!IsOpen || SceneTransition.Busy) return;
        IsOpen = false;
        panel.transform.DOKill(true);
        panel.SetActive(false);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null);
        SetGameState(GameState.Playing);
    }

    // With keys or a gamepad keep a button picked (Resume first); with the mouse, hovering shows it
    private void KeepSelection()
    {
        EventSystem events = EventSystem.current;
        if (events == null) return;

        GameObject current = events.currentSelectedGameObject;
        if (!UINavigation.UsingKeys)
        {
            if (current != null)
                events.SetSelectedGameObject(null);
        }
        else if ((current == null || !current.activeInHierarchy) && resumeButton != null)
            events.SetSelectedGameObject(resumeButton.gameObject);

        GameObject selected = events.currentSelectedGameObject;
        if (selected == lastSelected) return;
        if (lastSelected != null)
            lastSelected.transform.DOScale(1f, 0.1f).SetUpdate(true).SetLink(lastSelected);
        if (selected != null)
            selected.transform.DOScale(1.1f, 0.1f).SetUpdate(true).SetLink(selected);
        lastSelected = selected;
    }

    private static void SetGameState(GameState state)
    {
        var gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager != null)
            gameManager.SetGameState(state);
        else
        {
            GameStateManager.SetState(state);
            Time.timeScale = state == GameState.Paused ? 0f : 1f;
        }
    }
}
