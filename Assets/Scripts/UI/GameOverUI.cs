using UnityEngine;
using UnityEngine.SceneManagement;

// The game-over screen's buttons: both change scene behind the diamond transition, staying paused until
// the screen is covered
public class GameOverUI : MonoBehaviour
{
    [Tooltip("Seconds the diamonds take to cover the screen, and again to clear off the next scene.")]
    [SerializeField] private float transitionTime = 0.6f;

    public void Retry()
    {
        // Reload the current active scene
        Load(SceneManager.GetActiveScene().name);
    }

    public void QuitToMenu() => Load(MainMenu.SceneName);

    private void Load(string sceneName)
    {
        if (SceneTransition.Busy) return;
        // The shop's background, so the diamonds look like the game's own
        var look = FindAnyObjectByType<ScrollingBackground>(FindObjectsInactive.Include);
        SceneTransition.Load(sceneName, transitionTime, look, () =>
        {
            Time.timeScale = 1f;
            GameStateManager.ResetState();
        });
    }
}
