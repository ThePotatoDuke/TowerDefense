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
    [Tooltip("Picked when a key or gamepad is first used.")]
    [SerializeField] private Button firstButton;

    private void Start()
    {
        // Coming back from a run that ended paused or mid-game-over
        Time.timeScale = 1f;
        GameStateManager.ResetState();
        UINavigation.Reset();
    }

    private void Update()
    {
        // With keys or a gamepad keep something picked (a mouse click on empty space clears it); with the
        // mouse pick nothing, hovering shows it
        EventSystem events = EventSystem.current;
        if (events == null) return;
        if (!UINavigation.UsingKeys)
        {
            if (events.currentSelectedGameObject != null)
                events.SetSelectedGameObject(null);
        }
        else if (firstButton != null && events.currentSelectedGameObject == null)
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
