using System.Linq;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

// Tools > Create Pause Menu (with the game scene open): copies the game-over panel into a pause panel beside
// it, titled PAUSED, with a Resume button added to its Retry and Menu ones (they keep working as they do on
// the game-over panel), and puts a PauseMenu on their canvas pointing at it. The three buttons are put in a
// row where the two were; lay the panel out as wanted after. Does nothing if the scene already has one.
public static class PauseMenuSetup
{
    [MenuItem("Tools/Create Pause Menu")]
    private static void Create()
    {
        PauseMenu existing = Object.FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Selection.activeObject = existing;
            Debug.Log("Pause menu: the scene already has one (selected).");
            return;
        }

        GameOverScreen gameOver = Object.FindAnyObjectByType<GameOverScreen>(FindObjectsInactive.Include);
        if (gameOver == null)
        {
            Debug.LogWarning("Pause menu: no game-over panel (GameOverScreen) in the open scene to copy.");
            return;
        }

        GameObject source = gameOver.gameObject;
        Transform canvas = source.transform.parent;
        GameObject panel = Object.Instantiate(source, canvas);
        Undo.RegisterCreatedObjectUndo(panel, "Create Pause Menu");
        panel.name = "PausePanel";
        panel.transform.SetSiblingIndex(source.transform.GetSiblingIndex() + 1);

        // The wave line, found in the copy by where it was in the original
        var waveSource = new SerializedObject(gameOver).FindProperty("waveText").objectReferenceValue as TMP_Text;
        TMP_Text waveText = waveSource != null ? Find<TMP_Text>(panel.transform, RelativePath(source.transform, waveSource.transform)) : null;
        Object.DestroyImmediate(panel.GetComponent<GameOverScreen>());

        foreach (TMP_Text text in panel.GetComponentsInChildren<TMP_Text>(true))
            if (text.text.Trim().ToUpperInvariant() == "GAME OVER")
                text.text = "PAUSED";

        Button retry = ButtonCalling(panel, "Retry");
        Button menu = ButtonCalling(panel, "QuitToMenu");

        PauseMenu pauseMenu = Undo.AddComponent<PauseMenu>(canvas.gameObject);

        Button resume = null;
        if (retry != null)
        {
            resume = Object.Instantiate(retry, retry.transform.parent);
            resume.name = "Resume";
            resume.transform.SetSiblingIndex(retry.transform.GetSiblingIndex());
            while (resume.onClick.GetPersistentEventCount() > 0)
                UnityEventTools.RemovePersistentListener(resume.onClick, 0);
            UnityEventTools.AddPersistentListener(resume.onClick, new UnityAction(pauseMenu.Resume));
            TMP_Text label = resume.GetComponentInChildren<TMP_Text>(true);
            if (label != null)
                label.text = "Resume";

            // In a row: Resume, Retry, Menu, spaced as the two were and centred where they were
            if (menu != null)
            {
                Vector3 first = retry.transform.position, step = menu.transform.position - first;
                Vector3 start = first - step * 0.5f;
                resume.transform.position = start;
                retry.transform.position = start + step;
                menu.transform.position = start + step * 2f;
            }
        }

        var serialized = new SerializedObject(pauseMenu);
        serialized.FindProperty("panel").objectReferenceValue = panel;
        serialized.FindProperty("resumeButton").objectReferenceValue = resume;
        serialized.FindProperty("waveText").objectReferenceValue = waveText;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        panel.SetActive(false);
        EditorSceneManager.MarkSceneDirty(panel.scene);
        Selection.activeObject = panel;
        Debug.Log("Pause menu: made PausePanel next to the game-over panel and a PauseMenu on its canvas. " +
                  "Lay it out as wanted (it's switched off; tick it on to see it), then save the scene.");
    }

    // The button whose click calls this method (set in the scene)
    private static Button ButtonCalling(GameObject root, string method) =>
        root.GetComponentsInChildren<Button>(true).FirstOrDefault(button =>
            Enumerable.Range(0, button.onClick.GetPersistentEventCount())
                .Any(i => button.onClick.GetPersistentMethodName(i) == method));

    private static string RelativePath(Transform root, Transform child)
    {
        string path = "";
        for (Transform at = child; at != null && at != root; at = at.parent)
            path = path.Length == 0 ? at.name : at.name + "/" + path;
        return path;
    }

    private static T Find<T>(Transform root, string path) where T : Component
    {
        Transform found = path.Length == 0 ? root : root.Find(path);
        return found != null ? found.GetComponent<T>() : null;
    }
}
