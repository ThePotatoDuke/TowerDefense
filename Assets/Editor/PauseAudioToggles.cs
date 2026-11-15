using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

// Tools > Add Pause Audio Toggles (with the game scene open): puts a music toggle and a sound effects toggle
// on the pause panel, side by side under its buttons, their art pixels the size of the panel's. Each sprite
// sheet has the on button first and the off one second. Running it again puts them back in place.
public static class PauseAudioToggles
{
    private const string MusicPath = "Assets/3rd Party/UI/music_toggle.png";
    private const string SfxPath = "Assets/3rd Party/UI/sfx_toggle-export.png";
    private const float GapPixels = 4f; // art pixels between the two, and between them and the buttons

    [MenuItem("Tools/Add Pause Audio Toggles")]
    private static void Add()
    {
        PauseMenu pauseMenu = Object.FindAnyObjectByType<PauseMenu>(FindObjectsInactive.Include);
        var panel = pauseMenu != null
            ? new SerializedObject(pauseMenu).FindProperty("panel").objectReferenceValue as GameObject : null;
        Sprite[] music = Frames(MusicPath), sfx = Frames(SfxPath);
        if (panel == null || music == null || sfx == null)
        {
            Debug.LogWarning("Pause audio toggles: needs the pause panel in the open scene (Tools > Create Pause Menu) " +
                             $"and two sprites each in {MusicPath} and {SfxPath}.");
            return;
        }

        Image board = panel.GetComponent<Image>();
        float pixel = board != null ? UIPixels.PixelSize(board) : UIPixels.Scale;
        Button[] buttons = panel.GetComponentsInChildren<Button>(true)
            .Where(button => button.GetComponent<AudioToggle>() == null).ToArray();

        RectTransform panelRect = (RectTransform)panel.transform;
        // The lowest edge of the buttons, in the panel's space (its middle if there are none)
        float below = 0f;
        if (buttons.Length > 0)
        {
            below = float.MaxValue;
            var corners = new Vector3[4];
            foreach (Button button in buttons)
            {
                ((RectTransform)button.transform).GetWorldCorners(corners);
                below = Mathf.Min(below, panelRect.InverseTransformPoint(corners[0]).y);
            }
        }

        Vector2 size = music[0].rect.size * pixel;
        float gap = GapPixels * pixel;
        float y = below - gap - size.y * 0.5f;
        RectTransform musicToggle = MakeToggle(panelRect, "MusicToggle", AudioToggle.Kind.Music, music, size, buttons);
        RectTransform sfxToggle = MakeToggle(panelRect, "SfxToggle", AudioToggle.Kind.Sfx, sfx, size, buttons);
        musicToggle.localPosition = new Vector3(-(size.x + gap) * 0.5f, y, 0f);
        sfxToggle.localPosition = new Vector3((size.x + gap) * 0.5f, y, 0f);

        EditorSceneManager.MarkSceneDirty(panel.scene);
        Selection.activeObject = musicToggle.gameObject;
        Debug.Log("Pause audio toggles: added under the pause panel's buttons. Move them as wanted (tick the " +
                  "panel on to see it), then save the scene.");
    }

    private static RectTransform MakeToggle(RectTransform panel, string name, AudioToggle.Kind kind, Sprite[] frames,
                                            Vector2 size, Button[] buttons)
    {
        Transform existing = panel.Find(name);
        GameObject toggle;
        if (existing != null)
        {
            toggle = existing.gameObject;
            Undo.RecordObjects(toggle.GetComponents<Component>(), "Add Pause Audio Toggles");
        }
        else
        {
            toggle = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button),
                                    typeof(AudioToggle));
            Undo.RegisterCreatedObjectUndo(toggle, "Add Pause Audio Toggles");
            toggle.transform.SetParent(panel, false);
        }

        var rect = (RectTransform)toggle.transform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;

        var image = toggle.GetComponent<Image>();
        image.sprite = frames[0];
        image.preserveAspect = true;

        // Pressed and hovered like the panel's other buttons
        var button = toggle.GetComponent<Button>();
        button.targetGraphic = image;
        if (buttons.Length > 0)
        {
            button.transition = buttons[0].transition;
            button.colors = buttons[0].colors;
        }

        var serialized = new SerializedObject(toggle.GetComponent<AudioToggle>());
        serialized.FindProperty("kind").enumValueIndex = (int)kind;
        serialized.FindProperty("image").objectReferenceValue = image;
        serialized.FindProperty("onSprite").objectReferenceValue = frames[0];
        serialized.FindProperty("offSprite").objectReferenceValue = frames[1];
        serialized.ApplyModifiedPropertiesWithoutUndo();
        return rect;
    }

    // The sheet's two sprites, left (on) then right (off)
    private static Sprite[] Frames(string path)
    {
        Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(sprite => sprite.rect.x).ToArray();
        return sprites.Length >= 2 ? new[] { sprites[0], sprites[1] } : null;
    }
}
