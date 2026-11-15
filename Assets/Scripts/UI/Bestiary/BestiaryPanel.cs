using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

// The bestiary: a grid of every enemy (BestiaryData), the ones not met yet as dark shapes, and the one
// picked (hovered, clicked, or chosen with keys or a gamepad) shown large and animated beside it, with its
// health, damage, traits, what it does and a tip. Opened from the main menu or the pause menu; Esc, Back
// or a gamepad's B closes it. Runs on real time, so it works while the game's paused. Laid out in
// the main menu and game scenes.
public class BestiaryPanel : MonoBehaviour
{
    public static bool IsOpen { get; private set; }
    // The frame it was closed in, so the Esc that closed it doesn't also close the pause menu
    public static int ClosedFrame { get; private set; } = -1;

    [SerializeField] private GameObject panel;
    [Tooltip("Pops in when opened.")]
    [SerializeField] private RectTransform board;
    [SerializeField] private ScrollRect scroll;
    [SerializeField] private RectTransform grid;
    [Tooltip("Copied for each enemy (kept switched off).")]
    [SerializeField] private BestiaryEntryButton entryTemplate;
    [SerializeField] private Button closeButton;
    [SerializeField] private TMP_Text progressText;

    [Header("Details")]
    [SerializeField] private Image portrait;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text statsText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text tipText;

    [Header("Look")]
    [Tooltip("An enemy not met yet is drawn in this colour, a shape only.")]
    [SerializeField] private Color unseenColor = new(0.12f, 0.1f, 0.16f, 1f);
    [SerializeField] private string unseenName = "???";
    [SerializeField] private string unseenDescription = "Not met yet. It'll be written up here once it's turned up in a run.";
    [SerializeField] private float popTime = 0.25f;
    [Tooltip("Canvas units per art pixel for the large picture: every enemy the same, so they show their " +
             "sizes next to each other (one too big for its box is drawn smaller to fit).")]
    [SerializeField] private float portraitScale = 8f;
    [Tooltip("The same for the grid's squares.")]
    [SerializeField] private float iconScale = 4f;

    private readonly List<BestiaryEntryButton> buttons = new();
    private BestiaryData.Entry shown;
    private bool shownSeen;
    private float frameTime;
    private int frame;
    private Vector2 portraitBox;
    private GameObject lastSelected;
    private GameObject openedFrom;

    private void Awake()
    {
        IsOpen = false;
        if (panel != null)
            panel.SetActive(false);
        if (entryTemplate != null)
            entryTemplate.gameObject.SetActive(false);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (portrait != null)
            portraitBox = portrait.rectTransform.sizeDelta;
    }

    private void OnDestroy() => IsOpen = false;

    public void Open()
    {
        if (IsOpen || panel == null || SceneTransition.Busy) return;
        BestiaryData data = BestiaryData.Instance;
        if (data == null)
        {
            Debug.LogWarning("Bestiary: no Resources/Bestiary asset.");
            return;
        }

        IsOpen = true;
        openedFrom = EventSystem.current != null ? EventSystem.current.currentSelectedGameObject : null;
        Fill(data);

        panel.transform.SetAsLastSibling();
        panel.SetActive(true);
        if (board != null)
        {
            board.DOKill(true);
            board.localScale = Vector3.zero;
            board.DOScale(1f, popTime).SetEase(Ease.OutBack).SetUpdate(true).SetLink(board.gameObject);
        }
        Sounds.Play(Sound.Pause);

        if (scroll != null)
            scroll.verticalNormalizedPosition = 1f;
        Show(0);
        lastSelected = null;
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(UINavigation.UsingKeys && buttons.Count > 0 ? buttons[0].gameObject : null);
    }

    public void Close()
    {
        if (!IsOpen) return;
        IsOpen = false;
        ClosedFrame = Time.frameCount;
        if (board != null)
            board.DOKill(true);
        panel.SetActive(false);
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(UINavigation.UsingKeys ? openedFrom : null);
    }

    // One button per enemy, made the first time and brought up to date (met or not) every time
    private void Fill(BestiaryData data)
    {
        if (entryTemplate == null || grid == null) return;

        while (buttons.Count < data.entries.Count)
        {
            BestiaryEntryButton button = Instantiate(entryTemplate, grid);
            button.Set(this, buttons.Count);
            buttons.Add(button);
        }

        int met = 0;
        for (int i = 0; i < buttons.Count; i++)
        {
            bool used = i < data.entries.Count;
            buttons[i].gameObject.SetActive(used);
            if (!used) continue;
            BestiaryData.Entry entry = data.entries[i];
            bool seen = IsSeen(data, entry);
            if (seen) met++;
            Sprite first = entry.frames != null && entry.frames.Length > 0 ? entry.frames[0] : null;
            buttons[i].ShowIcon(first, seen ? Color.white : unseenColor, iconScale);
        }

        if (progressText != null)
            progressText.text = $"{met} / {data.entries.Count} met";
    }

    private static bool IsSeen(BestiaryData data, BestiaryData.Entry entry) =>
        data.showAll || BestiaryProgress.IsSeen(entry.Data);

    // The entry's details beside the grid
    public void Show(int index)
    {
        BestiaryData data = BestiaryData.Instance;
        if (data == null || index < 0 || index >= data.entries.Count) return;

        shown = data.entries[index];
        shownSeen = IsSeen(data, shown);
        frame = 0;
        frameTime = 0f;
        for (int i = 0; i < buttons.Count; i++)
            buttons[i].SetPicked(i == index);

        if (portrait != null)
        {
            Sprite first = shown.frames != null && shown.frames.Length > 0 ? shown.frames[0] : null;
            portrait.enabled = first != null;
            portrait.color = shownSeen ? Color.white : unseenColor;
            if (first != null)
                FitPixels(portrait, first, portraitBox, portraitScale);
        }

        if (nameText != null)
            nameText.text = shownSeen ? shown.displayName : unseenName;
        if (statsText != null)
            statsText.text = shownSeen ? Stats(data, shown) : "";
        if (descriptionText != null)
            descriptionText.text = shownSeen ? shown.description : unseenDescription;
        if (tipText != null)
            tipText.text = shownSeen && !string.IsNullOrEmpty(shown.tip) ? "Tip: " + shown.tip : "";
    }

    // Health and damage from its data, and what's special about it
    private static string Stats(BestiaryData data, BestiaryData.Entry entry)
    {
        EnemyDataSO enemy = entry.Data;
        if (enemy == null) return "";

        var text = new StringBuilder();
        text.Append($"Health {enemy.maxHealth:0}    Damage {enemy.contactDamage:0}");

        var traits = new List<string>();
        if (entry.prefab is BatEnemy or FrillsEnemy)
            traits.Add("Flies");
        if (enemy.dropsFromSky)
            traits.Add("Drops from the sky");
        if (entry.prefab.Invulnerable)
            traits.Add("Can't be hurt");
        if (enemy.splitInto != null)
        {
            BestiaryData.Entry piece = data.entries.Find(other => other.prefab == enemy.splitInto);
            string pieceName = piece != null && IsSeen(data, piece) ? piece.displayName : "smaller ones";
            traits.Add($"Splits into {enemy.splitCount} {pieceName}");
        }
        if (traits.Count > 0)
            text.Append('\n').Append(string.Join("  ·  ", traits));
        return text.ToString();
    }

    // Its art pixels this many units each, or the most whole units that fit the box if that's too big, so
    // it stays crisp
    public static void FitPixels(Image image, Sprite sprite, Vector2 box, float scale)
    {
        image.sprite = sprite;
        Vector2 size = sprite.rect.size;
        float fit = Mathf.Floor(Mathf.Min(box.x / size.x, box.y / size.y));
        image.rectTransform.sizeDelta = size * Mathf.Max(1f, Mathf.Min(scale, fit));
    }

    private void Update()
    {
        if (!IsOpen) return;

        if (BackPressed())
        {
            Close();
            return;
        }

        Animate();
        KeepSelection();
    }

    private static bool BackPressed()
    {
        Keyboard keyboard = Keyboard.current;
        Gamepad gamepad = Gamepad.current;
        return keyboard != null && (keyboard.escapeKey.wasPressedThisFrame || keyboard.backspaceKey.wasPressedThisFrame)
               || gamepad != null && (gamepad.buttonEast.wasPressedThisFrame || gamepad.startButton.wasPressedThisFrame);
    }

    // The picked enemy's frames, on real time
    private void Animate()
    {
        if (portrait == null || shown == null || shown.frames == null || shown.frames.Length < 2) return;
        frameTime += Time.unscaledDeltaTime * Mathf.Max(0.1f, shown.framesPerSecond);
        if (frameTime < 1f) return;
        frameTime -= 1f;
        frame = (frame + 1) % shown.frames.Length;
        if (shown.frames[frame] != null)
            FitPixels(portrait, shown.frames[frame], portraitBox, portraitScale);
    }

    // With keys or a gamepad keep an enemy picked and scrolled into view; with the mouse, hovering picks
    private void KeepSelection()
    {
        EventSystem events = EventSystem.current;
        if (events == null) return;

        GameObject current = events.currentSelectedGameObject;
        if (!UINavigation.UsingKeys)
        {
            if (current != null)
                events.SetSelectedGameObject(null);
        }
        else if ((current == null || !current.activeInHierarchy || !current.transform.IsChildOf(panel.transform))
                 && buttons.Count > 0)
            events.SetSelectedGameObject(buttons[0].gameObject);

        GameObject selected = events.currentSelectedGameObject;
        if (selected == lastSelected) return;
        lastSelected = selected;
        if (selected != null && selected.transform.parent == grid)
            ScrollTo((RectTransform)selected.transform);
    }

    private void ScrollTo(RectTransform item)
    {
        if (scroll == null || scroll.viewport == null) return;
        Canvas.ForceUpdateCanvases();
        RectTransform viewport = scroll.viewport;
        Bounds itemBounds = RectTransformUtility.CalculateRelativeRectTransformBounds(viewport, item);
        Rect view = viewport.rect;
        float shift = 0f;
        if (itemBounds.max.y > view.yMax) shift = itemBounds.max.y - view.yMax;
        else if (itemBounds.min.y < view.yMin) shift = itemBounds.min.y - view.yMin;
        if (Mathf.Approximately(shift, 0f)) return;
        grid.anchoredPosition -= new Vector2(0f, shift);
    }
}
