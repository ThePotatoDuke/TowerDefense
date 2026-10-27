using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// The row of weapon slots: each shows the icon of the weapon carried in it, the one in hand stands out
// (raised, full colour) and gives a little punch when switched to, and empty slots stay faint. The slots
// themselves are laid out in the scene; Tools > Create UI Layout Objects builds a starting row.
public class WeaponBarUI : MonoBehaviour
{
    [Serializable]
    public class SlotView
    {
        public RectTransform root;
        public Image frame;
        public Image icon;
    }

    [SerializeField] private SlotView[] slots;
    [Tooltip("How far the slot in hand is raised (whole art pixels keep it crisp: 4 per pixel).")]
    [SerializeField] private float selectedLift = 8f;
    [Tooltip("Frame colour of slots holding a weapon that isn't in hand.")]
    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.6f);
    [Tooltip("Frame colour of slots with nothing in them.")]
    [SerializeField] private Color emptyColor = new Color(1f, 1f, 1f, 0.25f);

    private WeaponInventory inventory;
    private int shownIndex = -1;
    private float[] restingY;

    private void Start()
    {
        restingY = new float[slots.Length];
        for (int i = 0; i < slots.Length; i++)
            restingY[i] = slots[i] != null && slots[i].root != null ? slots[i].root.anchoredPosition.y : 0f;

        if (Player.Instance != null)
            inventory = Player.Instance.Inventory;
        if (inventory != null)
            inventory.OnChanged += Refresh;
        Refresh();
    }

    private void OnDestroy()
    {
        if (inventory != null)
            inventory.OnChanged -= Refresh;
    }

    private void Refresh()
    {
        int current = inventory != null ? inventory.CurrentIndex : -1;

        for (int i = 0; i < slots.Length; i++)
        {
            SlotView view = slots[i];
            if (view == null || view.root == null) continue;

            WeaponInventory.Slot slot = inventory != null && i < inventory.Slots.Count ? inventory.Slots[i] : null;
            bool selected = i == current;

            if (view.icon != null)
            {
                UIPixels.Show(view.icon, slot != null ? slot.weapon.icon : null);
                view.icon.color = selected ? Color.white : idleColor;
            }
            if (view.frame != null)
                view.frame.color = selected ? Color.white : slot != null ? idleColor : emptyColor;

            view.root.DOKill(true);
            view.root.anchoredPosition = new Vector2(view.root.anchoredPosition.x, restingY[i] + (selected ? selectedLift : 0f));
            if (selected && current != shownIndex)
                view.root.DOPunchScale(Vector3.one * 0.2f, 0.2f, 6, 0.5f).SetUpdate(true).SetLink(view.root.gameObject);
        }

        shownIndex = current;
    }
}
