using UnityEngine;
using UnityEngine.SceneManagement;

public class GameOverUI : MonoBehaviour
{
    public void Retry()
    {
        // Reload the current active scene
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        GameStateManager.ResetState();
    }

    public void QuitToMenu()
    {
        // Optional: load main menu scene
        SceneManager.LoadScene("MainMenu");
    }
}
