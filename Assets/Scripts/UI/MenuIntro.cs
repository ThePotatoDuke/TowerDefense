using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The main menu's opening: the clean title sign slides up from below the screen, flashes white and is
// the weathered sign when the flash fades, then the button signs drop out from behind it, each hanging
// from the one above by chains (children of the buttons, so they come along). Any key or click skips to
// the end. The buttons can't be clicked until it's over. Laid out by Tools > Set Up Main Menu Intro.
public class MenuIntro : MonoBehaviour
{
    [SerializeField] private Image sign;
    [Tooltip("A white copy of the sign over it, faded in and out for the flash.")]
    [SerializeField] private Image flash;
    [SerializeField] private Sprite cleanSign;
    [SerializeField] private Sprite dirtySign;
    [Tooltip("Top to bottom; each starts hidden behind the one above (the first behind the sign).")]
    [SerializeField] private Button[] buttons;

    [Header("Timing")]
    [SerializeField] private float startDelay = 0.3f;
    [SerializeField] private float slideTime = 0.8f;
    [SerializeField] private float pauseBeforeFlash = 0.25f;
    [SerializeField] private float flashInTime = 0.06f;
    [SerializeField] private float flashOutTime = 0.4f;
    [SerializeField] private float pauseBeforeDrop = 0.15f;
    [SerializeField] private float dropTime = 0.5f;
    [Tooltip("Seconds between one button starting to drop and the next.")]
    [SerializeField] private float dropGap = 0.25f;

    private Sequence intro;
    private CanvasGroup clicks;

    private void Start()
    {
        if (sign == null) return;

        RectTransform signRect = sign.rectTransform;
        Vector2 signRest = signRect.anchoredPosition;
        var canvas = GetComponentInParent<Canvas>();
        float screenHeight = canvas != null ? ((RectTransform)canvas.rootCanvas.transform).rect.height : 1080f;
        Vector2 belowScreen = Vector2.down * (screenHeight + signRect.rect.height);

        // No clicks until it's over (the buttons stay as they look, not greyed out)
        if (!TryGetComponent(out clicks))
            clicks = gameObject.AddComponent<CanvasGroup>();
        clicks.blocksRaycasts = false;

        // Starting places: the sign below the screen, each button tucked up behind the one above (and
        // below the screen with the sign until it's up)
        if (cleanSign != null)
            sign.sprite = cleanSign;
        if (flash != null)
            flash.color = new Color(1f, 1f, 1f, 0f);

        var rects = new RectTransform[buttons.Length];
        var rests = new Vector2[buttons.Length];
        var hidden = new Vector2[buttons.Length];
        for (int i = 0; i < buttons.Length; i++)
        {
            if (buttons[i] == null) continue;
            rects[i] = (RectTransform)buttons[i].transform;
            rests[i] = rects[i].anchoredPosition;
            // The first up behind the sign, the rest in the same place behind the one above
            hidden[i] = i == 0
                ? rests[i] + Vector2.up * (signRect.rect.height * 0.5f)
                : new Vector2(rests[i].x, hidden[i - 1].y);
        }

        intro = DOTween.Sequence().SetLink(gameObject);
        float time = startDelay;

        // Up from below
        signRect.anchoredPosition = signRest + belowScreen;
        intro.Insert(time, signRect.DOAnchorPos(signRest, slideTime).SetEase(Ease.OutCubic));
        for (int i = 0; i < rects.Length; i++)
        {
            if (rects[i] == null) continue;
            rects[i].anchoredPosition = hidden[i] + belowScreen;
            intro.Insert(time, rects[i].DOAnchorPos(hidden[i], slideTime).SetEase(Ease.OutCubic));
        }
        time += slideTime + pauseBeforeFlash;

        // Flash white, and it's the weathered sign under the flash
        if (flash != null)
            intro.Insert(time, flash.DOFade(1f, flashInTime));
        time += flashInTime;
        intro.InsertCallback(time, () => { if (dirtySign != null) sign.sprite = dirtySign; });
        intro.Insert(time, signRect.DOPunchScale(Vector3.one * 0.06f, 0.3f, 6, 0.5f));
        if (flash != null)
            intro.Insert(time, flash.DOFade(0f, flashOutTime));
        time += flashOutTime + pauseBeforeDrop;

        // Each drops a moment after the one above, from behind it, bouncing on its chains
        for (int i = 0; i < rects.Length; i++)
            if (rects[i] != null)
                intro.Insert(time + dropGap * i, rects[i].DOAnchorPos(rests[i], dropTime).SetEase(Ease.OutBounce));

        intro.OnComplete(() => clicks.blocksRaycasts = true);
    }

    private void Update()
    {
        if (intro == null || !intro.IsActive() || !intro.IsPlaying()) return;

        bool pressed = (Keyboard.current != null && Keyboard.current.anyKey.wasPressedThisFrame)
                       || (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                       || (Gamepad.current != null && Gamepad.current.buttonSouth.wasPressedThisFrame);
        if (pressed)
            intro.Complete(true); // straight to the end, callbacks and all
    }
}
