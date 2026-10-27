using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// The main menu scene (made by Tools > Create Main Menu): Play starts a run in the game scene, Quit closes
// the game (or stops play mode in the editor). The game-over screen's Main Menu button comes back here.
public class MainMenu : MonoBehaviour
{
    public const string SceneName = "MainMenu";

    [SerializeField] private string gameScene = "GameScene";
    [Tooltip("Picked when the menu opens, so keys and gamepads work straight away.")]
    [SerializeField] private Button firstButton;

    private void Start()
    {
        // Coming back from a run that ended paused or mid-game-over
        Time.timeScale = 1f;
        GameStateManager.ResetState();

        if (firstButton != null && EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(firstButton.gameObject);
    }

    private void Update()
    {
        // Keep something picked for keys and gamepads (a mouse click on empty space clears it)
        EventSystem events = EventSystem.current;
        if (firstButton != null && events != null && events.currentSelectedGameObject == null)
            events.SetSelectedGameObject(firstButton.gameObject);
    }

    public void Play() => SceneManager.LoadScene(gameScene);

    public void Quit()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
