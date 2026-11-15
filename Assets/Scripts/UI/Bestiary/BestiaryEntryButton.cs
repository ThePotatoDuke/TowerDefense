using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// One enemy's square in the bestiary's grid: its icon (a dark shape until it's been met), shown in the
// details when hovered, picked with keys or a gamepad, or clicked.
[RequireComponent(typeof(Button))]
public class BestiaryEntryButton : MonoBehaviour, IPointerEnterHandler, ISelectHandler
{
    [SerializeField] private Image icon;
    [Tooltip("Shown on the square whose enemy is in the details.")]
    [SerializeField] private GameObject pickedMark;

    private BestiaryPanel owner;
    private int index;
    private Vector2 iconBox;

    public void Set(BestiaryPanel panel, int entryIndex)
    {
        owner = panel;
        index = entryIndex;
        if (icon != null)
            iconBox = icon.rectTransform.sizeDelta;
        GetComponent<Button>().onClick.AddListener(() => owner.Show(index));
    }

    public void ShowIcon(Sprite sprite, Color color, float scale)
    {
        if (icon == null) return;
        icon.enabled = sprite != null;
        icon.color = color;
        if (sprite != null)
            BestiaryPanel.FitPixels(icon, sprite, iconBox, scale);
    }

    public void SetPicked(bool picked)
    {
        if (pickedMark != null)
            pickedMark.SetActive(picked);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (owner != null) owner.Show(index);
    }

    public void OnSelect(BaseEventData eventData)
    {
        if (owner != null) owner.Show(index);
    }
}
