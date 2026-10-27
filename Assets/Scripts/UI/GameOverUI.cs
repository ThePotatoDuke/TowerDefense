using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public void Retry()
    {
        // Reload the current active scene
        Time.timeScale = 1f;
        GameStateManager.ResetState();
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void QuitToMenu()
    {
        Time.timeScale = 1f;
        GameStateManager.ResetState();
        SceneManager.LoadScene(MainMenu.SceneName);
    }
}
