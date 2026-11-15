using UnityEngine;
using UnityEngine.UI;

// A button that turns the music or the sound effects on and off (AudioToggles), showing the on sprite or
// the off one. A pair sits on the pause menu.
[RequireComponent(typeof(Button))]
public class AudioToggle : MonoBehaviour
{
    public enum Kind { Music, Sfx }

    [SerializeField] private Kind kind;
    [SerializeField] private Image image;
    [SerializeField] private Sprite onSprite;
    [SerializeField] private Sprite offSprite;

    private bool On
    {
        get => kind == Kind.Music ? AudioToggles.MusicOn : AudioToggles.SfxOn;
        set
        {
            if (kind == Kind.Music) AudioToggles.MusicOn = value;
            else AudioToggles.SfxOn = value;
        }
    }

    private void Awake()
    {
        if (image == null)
            image = GetComponent<Image>();
        GetComponent<Button>().onClick.AddListener(Toggle);
    }

    private void OnEnable() => Refresh();

    private void Toggle()
    {
        On = !On;
        Refresh();
    }

    private void Refresh()
    {
        if (image != null)
            image.sprite = On ? onSprite : offSprite;
    }
}
