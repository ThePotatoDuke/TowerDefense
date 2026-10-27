using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

// The shop that opens every few waves (EndlessWaveManager asks for a Visit). The game pauses while it's
// open. It deals a few random upgrade cards (name, icon, what it does, level now and after, price in
// grass); buy as many as the grass allows, reroll the ones not bought (pricier each time in the same
// visit), and leave. Grass left over
// when it closes earns Savings Bloom's interest. Settings and the upgrade list are in ShopSettings; the
// window is laid out in the scene (Tools > Create Shop builds a starting one).
public class Shop : MonoBehaviour
{
    [Serializable]
    public class CardView
    {
        public Button button;
        public Image icon;
        public TMP_Text nameText;
        public TMP_Text descriptionText;
        public TMP_Text levelText;
        public TMP_Text priceText;
    }

    public static Shop Instance { get; private set; }

    // After every visit (Spore Mines grow back)
    public static event Action OnClosed;

    [Tooltip("Everything shown while open (the dimmed background and the content).")]
    [SerializeField] private RectTransform window;
    [Tooltip("The title, cards and buttons; pops in when the shop opens.")]
    [SerializeField] private RectTransform content;
    [SerializeField] private CardView[] cards;
    [SerializeField] private Button rerollButton;
    [SerializeField] private TMP_Text rerollText;
    [SerializeField] private Button skipButton;
    [Tooltip("Price text colour when there isn't enough grass.")]
    [SerializeField] private Color tooExpensiveColor = new Color(0.75f, 0.2f, 0.2f);
    [Tooltip("How much the card or button picked with the keys or mouse grows.")]
    [SerializeField] private float selectedScale = 1.08f;

    private readonly List<UpgradeDefinition> offers = new();
    private readonly HashSet<int> sold = new();
    private bool open;
    private int purchases;
    private int rerolls;
    private Color priceColor = Color.white;
    private GameObject lastSelected;

    private ShopSettings Settings => ShopSettings.Instance;

    private void Awake()
    {
        Instance = this;
        if (cards.Length > 0 && cards[0].priceText != null)
            priceColor = cards[0].priceText.color;

        for (int i = 0; i < cards.Length; i++)
        {
            int index = i;
            if (cards[i].button != null)
                cards[i].button.onClick.AddListener(() => Buy(index));
        }
        if (rerollButton != null) rerollButton.onClick.AddListener(Reroll);
        if (skipButton != null) skipButton.onClick.AddListener(Close);

        if (window != null)
            window.gameObject.SetActive(false);
    }

    // Testing: the upgrades listed in the settings, for free
    private void Start()
    {
        if (Settings == null) return;
        foreach (UpgradeType type in Settings.testStartingUpgrades)
        {
            UpgradeDefinition upgrade = Settings.Get(type);
            if (upgrade == null) continue;
            Upgrades.Add(type);
            ApplyNow(upgrade);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    // Opens the shop and waits until it's closed
    public IEnumerator Visit()
    {
        if (Settings == null || window == null) yield break;

        open = true;
        purchases = 0;
        rerolls = 0;
        SetPaused(true);

        window.gameObject.SetActive(true);
        if (content != null)
        {
            content.DOKill(true);
            content.localScale = Vector3.one * 0.8f;
            content.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetUpdate(true).SetLink(content.gameObject);
        }

        Deal(false);
        Currency.OnGrassChanged += HandleGrassChanged;

        yield return new WaitUntil(() => !open);

        Currency.OnGrassChanged -= HandleGrassChanged;
        window.gameObject.SetActive(false);
        PaySavingsInterest();
        SetPaused(false);
        OnClosed?.Invoke();
    }

    private void Update()
    {
        if (!open) return;

        // Keep something picked for keys and gamepad (a mouse click on empty space clears it)
        EventSystem events = EventSystem.current;
        if (events != null && (events.currentSelectedGameObject == null || !events.currentSelectedGameObject.activeInHierarchy))
            SelectFirst();

        GameObject selected = events != null ? events.currentSelectedGameObject : null;
        if (selected != lastSelected)
        {
            if (lastSelected != null)
                lastSelected.transform.DOScale(1f, 0.1f).SetUpdate(true).SetLink(lastSelected);
            if (selected != null)
                selected.transform.DOScale(selectedScale, 0.1f).SetUpdate(true).SetLink(selected);
            lastSelected = selected;
        }
    }

    private void SetPaused(bool paused)
    {
        GameState state = paused ? GameState.Paused : GameState.Playing;
        var gameManager = FindAnyObjectByType<GameManager>();
        if (gameManager != null)
            gameManager.SetGameState(state);
        else
        {
            GameStateManager.SetState(state);
            Time.timeScale = paused ? 0f : 1f;
        }
    }

    // Deals random cards into the slots. A reroll only replaces the cards not bought yet (bought ones
    // stay, marked sold) and avoids the cards just shown when there are enough others; the same upgrade
    // is never on show twice.
    private void Deal(bool reroll)
    {
        int count = Mathf.Min(Settings.offersPerVisit, cards.Length);
        if (!reroll)
        {
            offers.Clear();
            sold.Clear();
        }
        while (offers.Count < count)
            offers.Add(null);

        var previous = new List<UpgradeDefinition>(offers);
        List<UpgradeDefinition> pool = Settings.upgrades.FindAll(IsAvailable);
        foreach (int index in sold)
            pool.Remove(offers[index]);
        if (reroll && pool.Count - previous.Count >= count - sold.Count)
            pool.RemoveAll(previous.Contains);

        var dealt = new List<int>();
        for (int i = 0; i < count; i++)
        {
            if (sold.Contains(i)) continue;
            UpgradeDefinition pick = pool.Count > 0 ? PickWeighted(pool) : null;
            pool.Remove(pick);
            offers[i] = pick;
            dealt.Add(i);
        }

        for (int i = 0; i < cards.Length; i++)
        {
            if (cards[i].button == null) continue;
            bool shown = i < offers.Count && offers[i] != null;
            cards[i].button.gameObject.SetActive(shown);
            if (!shown || !dealt.Contains(i)) continue;

            Transform card = cards[i].button.transform;
            card.DOKill(true);
            card.localScale = Vector3.zero;
            card.DOScale(1f, 0.25f).SetEase(Ease.OutBack).SetDelay(0.05f * i).SetUpdate(true).SetLink(card.gameObject);
        }
        lastSelected = null;

        Refresh();
        SelectFirst();
    }

    private bool IsAvailable(UpgradeDefinition upgrade)
    {
        if (upgrade == null || upgrade.weight <= 0f) return false;
        if (upgrade.HasLevels && Upgrades.Level(upgrade.type) >= upgrade.maxLevel) return false;

        switch (upgrade.type)
        {
            case UpgradeType.Heal:
                return Player.Instance != null && Player.Instance.IsHurt;
            case UpgradeType.CastleHeal:
                var castle = FindAnyObjectByType<CastleHealth>();
                return castle != null && castle.IsDamaged;
            case UpgradeType.Totem: // one at a time
                return TotemAbility.Instance != null && !TotemAbility.Instance.HasTotem;
            case UpgradeType.CrimsonWeed: // one seed carried at a time
                return WeedPlanter.Instance != null && !WeedPlanter.Instance.HasSeed;
            default:
                return true;
        }
    }

    private static UpgradeDefinition PickWeighted(List<UpgradeDefinition> pool)
    {
        float total = 0f;
        foreach (UpgradeDefinition upgrade in pool)
            total += upgrade.weight;

        float roll = UnityEngine.Random.value * total;
        foreach (UpgradeDefinition upgrade in pool)
        {
            roll -= upgrade.weight;
            if (roll <= 0f) return upgrade;
        }
        return pool[pool.Count - 1];
    }

    // Pricier with every one bought (levels, or more of the same, like weeds)
    private int Price(UpgradeDefinition upgrade) => upgrade.CostAt(Upgrades.Level(upgrade.type));

    private int RerollPrice => Settings.rerollCost + Settings.rerollCostStep * rerolls;

    private void HandleGrassChanged(int grass) => Refresh();

    // Card texts, prices and which buttons can be pressed
    private void Refresh()
    {
        for (int i = 0; i < offers.Count && i < cards.Length; i++)
        {
            CardView card = cards[i];
            UpgradeDefinition upgrade = offers[i];
            if (upgrade == null) continue;
            int level = Upgrades.Level(upgrade.type);
            int price = Price(upgrade);
            bool isSold = sold.Contains(i);
            bool affordable = Currency.Grass >= price;

            if (card.icon != null)
            {
                UIPixels.Show(card.icon, upgrade.icon);
            }
            if (card.nameText != null) card.nameText.text = upgrade.displayName;
            if (card.descriptionText != null) card.descriptionText.text = upgrade.Description;
            if (card.levelText != null)
                card.levelText.text = upgrade.HasLevels ? $"Lv {level} → {level + 1}" : "";
            if (card.priceText != null)
            {
                card.priceText.text = isSold ? "Sold" : price.ToString();
                card.priceText.color = isSold || affordable ? priceColor : tooExpensiveColor;
            }
            if (card.button != null)
                card.button.interactable = !isSold && affordable;
        }

        if (rerollText != null)
            rerollText.text = $"Reroll {RerollPrice}";
        if (rerollButton != null)
            rerollButton.interactable = Currency.Grass >= RerollPrice && sold.Count < offers.Count; // something left to reroll
    }

    private void SelectFirst()
    {
        EventSystem events = EventSystem.current;
        if (events == null) return;

        foreach (CardView card in cards)
        {
            if (card.button != null && card.button.gameObject.activeInHierarchy && card.button.interactable)
            {
                events.SetSelectedGameObject(card.button.gameObject);
                return;
            }
        }
        if (rerollButton != null && rerollButton.interactable)
            events.SetSelectedGameObject(rerollButton.gameObject);
        else if (skipButton != null)
            events.SetSelectedGameObject(skipButton.gameObject);
    }

    private void Buy(int index)
    {
        if (!open || index >= offers.Count || offers[index] == null || sold.Contains(index)) return;

        UpgradeDefinition upgrade = offers[index];
        if (!Currency.TrySpendGrass(Price(upgrade))) return;

        Upgrades.Add(upgrade.type);
        ApplyNow(upgrade);
        purchases++;

        if (Settings.purchasesPerVisit > 0 && purchases >= Settings.purchasesPerVisit)
        {
            Close();
            return;
        }

        sold.Add(index);
        Refresh();
        if (EventSystem.current != null)
            EventSystem.current.SetSelectedGameObject(null); // Update picks the next one
    }

    // The upgrades that do something the moment they're bought; the rest are read where they apply
    private static void ApplyNow(UpgradeDefinition upgrade)
    {
        Player player = Player.Instance;
        switch (upgrade.type)
        {
            case UpgradeType.MaxHealth:
                if (player != null) player.AddMaxHealth(upgrade.valuePerLevel);
                break;
            case UpgradeType.Heal:
                if (player != null)
                    player.Heal(upgrade.isPercent ? player.MaxHealth * upgrade.valuePerLevel / 100f : upgrade.valuePerLevel);
                break;
            case UpgradeType.CastleHeal:
                var castle = FindAnyObjectByType<CastleHealth>();
                if (castle != null)
                    castle.Heal(upgrade.isPercent ? castle.MaxHealth * upgrade.valuePerLevel / 100f : upgrade.valuePerLevel);
                break;
            case UpgradeType.SporeMines:
                SporeMines.AddMushroom();
                break;
            case UpgradeType.Totem:
                if (TotemAbility.Instance != null) TotemAbility.Instance.Give();
                break;
            case UpgradeType.CrimsonWeed:
                if (WeedPlanter.Instance != null) WeedPlanter.Instance.GiveSeed();
                break;
        }
    }

    private void Reroll()
    {
        if (!open || !Currency.TrySpendGrass(RerollPrice)) return;
        rerolls++;
        Deal(true);
    }

    private void Close() => open = false;

    // Savings Bloom: unspent grass grows by its percentage when the shop closes
    private static void PaySavingsInterest()
    {
        float percent = Upgrades.Bonus(UpgradeType.SavingsBloom);
        int interest = Mathf.FloorToInt(Currency.Grass * percent / 100f);
        if (interest > 0)
            Currency.AddGrass(interest);
    }
}
