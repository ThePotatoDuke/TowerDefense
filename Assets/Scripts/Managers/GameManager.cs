using UnityEngine;
using Unity.Cinemachine;
using System.Collections;
using UnityEngine.Playables;
public class GameManager : MonoBehaviour
{
    [Header("Cameras")]
    [SerializeField] private CinemachineCamera playerCamera;
    [SerializeField] private CinemachineCamera castleCamera;

    [Header("UI")]
    [SerializeField] private GameObject gameOverPopup;

    [SerializeField] private PlayableDirector gameOverTimeline;

    // GameStateManager is the single source of truth; GameManager drives it
    public GameState CurrentState => GameStateManager.CurrentState;

    private void Awake()
    {
        // Hide GameOver popup at start
        if (gameOverPopup != null)
            gameOverPopup.SetActive(false);

        // Set game state to Playing
        SetGameState(GameState.Playing);
    }

    private void OnEnable()
    {
        GameEvents.OnPlayerDied += HandlePlayerDeath;
        GameEvents.OnCastleDied += HandleCastleDeath;
    }

    private void OnDisable()
    {
        GameEvents.OnPlayerDied -= HandlePlayerDeath;
        GameEvents.OnCastleDied -= HandleCastleDeath;
    }

    public void SetGameState(GameState newState)
    {
        GameStateManager.SetState(newState);

        switch (newState)
        {
            case GameState.Playing:
                Time.timeScale = 1f;
                if (gameOverPopup != null)
                    gameOverPopup.SetActive(false);
                break;
            case GameState.PlayerDead:
            case GameState.CastleDestroyed:
                Time.timeScale = 1f; // keep normal so coroutines work
                if (newState == GameState.CastleDestroyed && castleCamera != null)
                    CameraManager.SwitchCamera(castleCamera);
                break;
            case GameState.Paused:
                Time.timeScale = 0f;
                break;
        }
    }

    private void HandlePlayerDeath()
    {
        if (CurrentState != GameState.Playing) return;

        SetGameState(GameState.PlayerDead);

        if (gameOverPopup != null)
            gameOverPopup.SetActive(true);

        Debug.Log("Player died! Game Over popup shown.");
    }

    private void HandleCastleDeath()
    {
        if (CurrentState != GameState.Playing) return;

        SetGameState(GameState.CastleDestroyed);

        // Show the popup once the castle has finished collapsing into the ground
        float collapse = FXSettings.Instance != null ? FXSettings.Instance.CastleCollapseDuration : 2.2f;
        StartCoroutine(ShowPopupAfterDelay(collapse + 0.8f));
    }

    private IEnumerator ShowPopupAfterDelay(float delay)
    {
        yield return new WaitForSecondsRealtime(delay); // ignore timeScale
        if (gameOverPopup != null)
            gameOverPopup.SetActive(true);

        Debug.Log("Castle died! Game Over popup shown after delay.");
    }
}
