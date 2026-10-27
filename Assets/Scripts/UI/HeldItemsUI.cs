using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Icons for the one-off things bought in the shop while they're held: the Totem (top right; while it
// channels the icon fills back in round from dim) and a Crimson Weed seed waiting to be planted. Each
// pops in when gained and out when used. Laid out in the scene; Tools > Create Shop builds a starting one.
public class HeldItemsUI : MonoBehaviour
{
    [SerializeField] private RectTransform totemIcon;
    [Tooltip("The bright totem icon, a radial fill over a dim copy: full normally, sweeping round while " +
             "it channels.")]
    [SerializeField] private Image totemCharge;
    [SerializeField] private RectTransform seedIcon;

    private TotemAbility totem;
    private WeedPlanter planter;

    private void Start()
    {
        totem = TotemAbility.Instance;
        planter = WeedPlanter.Instance;
        if (totem != null) totem.OnChanged += Refresh;
        if (planter != null) planter.OnChanged += Refresh;

        // Hidden to start, without the pop
        if (totemIcon != null) totemIcon.gameObject.SetActive(false);
        if (seedIcon != null) seedIcon.gameObject.SetActive(false);
        Refresh();
    }

    private void OnDestroy()
    {
        if (totem != null) totem.OnChanged -= Refresh;
        if (planter != null) planter.OnChanged -= Refresh;
    }

    private void Update()
    {
        if (totemCharge != null)
            totemCharge.fillAmount = totem != null && totem.IsChannelling ? totem.ChannelProgress : 1f;
    }

    private void Refresh()
    {
        Show(totemIcon, totem != null && totem.HasTotem);
        Show(seedIcon, planter != null && planter.HasSeed);
    }

    private static void Show(RectTransform icon, bool show)
    {
        if (icon == null) return;

        if (show)
        {
            if (!icon.gameObject.activeSelf)
            {
                icon.gameObject.SetActive(true);
                icon.localScale = Vector3.zero;
            }
            icon.DOKill();
            icon.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(icon.gameObject);
        }
        else if (icon.gameObject.activeSelf)
        {
            icon.DOKill();
            icon.DOScale(0f, 0.15f).SetEase(Ease.InBack).SetUpdate(true).SetLink(icon.gameObject)
                .OnComplete(() => icon.gameObject.SetActive(false));
        }
    }
}
