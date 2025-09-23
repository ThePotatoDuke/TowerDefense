using UnityEngine;
using UnityEngine.SceneManagement;

public class NextScene : MonoBehaviour
{
    void Start()
    {
        // Start a coroutine to load the scene after 1 second
        StartCoroutine(LoadSceneAfterDelay(1f));
    }

    private System.Collections.IEnumerator LoadSceneAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        SceneManager.LoadScene("GameScene");
    }
}
