using System;
using System.Collections.Generic;
using UnityEngine;

public enum UpgradeType
{
    MaxHealth,
    Heal,
    MoveSpeed,
    GrassMagnet,
    SwordDamage,
    SwordAttackRate,
    SwordSize,
    BlasterDamage,
    BlasterFireRate,
    BlasterRange,
    HoopCannonDamage,
    HoopCannonFireRate,
    HoopCannonRange,
    CastleHeal,
    DropRate,
    GrowthRate,
    SavingsBloom,
    SporeMines,
    Totem,
    CrimsonWeed
}

// One thing the shop can offer. Most stack in levels up to the max; with a max level of 0 it can be
// bought any number of times (the heals).
[Serializable]
public class UpgradeDefinition
{
    public UpgradeType type;
    public string displayName;
    public Sprite icon;
    [Tooltip("Card text; {value} is replaced by what one level gives, e.g. \"Sword hits {value} harder\".")]
    [TextArea(2, 3)] public string description;
    [Tooltip("What one level gives: a percentage when Is Percent is ticked, otherwise an amount.")]
    public float valuePerLevel = 10f;
    public bool isPercent = true;
    [Tooltip("0 = no limit and no levels (bought as often as wanted; still pricier each time with Cost Growth).")]
    public int maxLevel = 5;
    [Tooltip("Price of the first level, in grass.")]
    public int baseCost = 30;
    [Tooltip("Each level costs this much more than the one before (1.5 = +50%).")]
    public float costGrowth = 1.4f;
    [Tooltip("How likely it is to be offered, compared to the others (lower = rarer).")]
    public float weight = 1f;

    public bool HasLevels => maxLevel > 0;

    public int CostAt(int level) => Mathf.RoundToInt(baseCost * Mathf.Pow(costGrowth, level));

    public string FormattedValue => isPercent ? $"+{valuePerLevel:0.#}%" : $"+{valuePerLevel:0.#}";

    public string Description => (description ?? "").Replace("{value}", FormattedValue);
}

// Everything about the shop that opens between waves: how often, how many offers, reroll prices and the
// upgrades it can offer. Lives in Resources; Tools > Create Shop makes it with starting values.
[CreateAssetMenu(fileName = "ShopSettings", menuName = "Shop/Shop Settings")]
public class ShopSettings : ScriptableObject
{
    private static ShopSettings instance;
    public static ShopSettings Instance
    {
        get
        {
            if (instance == null)
                instance = Resources.Load<ShopSettings>("ShopSettings");
            return instance;
        }
    }

    [Header("Visits")]
    [Tooltip("The shop opens after every this many cleared waves.")]
    public int wavesBetweenShops = 3;
    [Tooltip("Seconds after a wave is cleared before the shop opens, so the clear is seen first.")]
    public float openDelay = 1.2f;
    public int offersPerVisit = 3;
    [Tooltip("How many cards can be bought each visit (0 = as many as the grass allows); the shop closes " +
             "once they're used up.")]
    public int purchasesPerVisit = 0;

    [Header("Reroll")]
    public int rerollCost = 10;
    [Tooltip("Each reroll in the same visit costs this much more than the last.")]
    public int rerollCostStep = 5;

    [Header("Upgrades")]
    public List<UpgradeDefinition> upgrades = new();

    [Header("Bought Things (their numbers are on the prefabs)")]
    [Tooltip("One Spore Mine mushroom (Prefabs/Shop/SporeMine).")]
    public SporeMine sporeMine;
    [Tooltip("Prefabs/Shop/Totem.")]
    public Totem totem;
    [Tooltip("The Crimson Weed planted from a seed (Prefabs/Shop/CrimsonWeed); its numbers are on the prefab.")]
    public CrimsonWeed crimsonWeed;

    [Header("Testing")]
    [Tooltip("Bought for free when the run starts, to try things out. Empty it for real play.")]
    public List<UpgradeType> testStartingUpgrades = new();

    public UpgradeDefinition Get(UpgradeType type) => upgrades.Find(upgrade => upgrade.type == type);
}
