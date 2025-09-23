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

    private bool isGameOver = false;

    private void Awake()
    {
        if (gameOverPopup != null)
            gameOverPopup.SetActive(false);
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

    private void HandlePlayerDeath()
    {
        if (isGameOver) return;
        isGameOver = true;

        if (gameOverPopup != null)
            gameOverPopup.SetActive(true);

        Debug.Log("Player died! Game Over popup shown.");
    }

    [SerializeField] private PlayableDirector gameOverTimeline;

    private void HandleCastleDeath()
    {
        if (isGameOver) return;
        isGameOver = true;

        // Switch to castle camera
        CameraManager.SwitchCamera(castleCamera);

        // Show popup after a short delay (e.g., 1 second)
        StartCoroutine(ShowPopupAfterDelay(3f));
    }

    private IEnumerator ShowPopupAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);

        if (gameOverPopup != null)
            gameOverPopup.SetActive(true);

        Debug.Log("Castle died! Game Over popup shown after delay.");
    }


}
