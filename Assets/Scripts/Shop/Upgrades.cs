using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

// The levels of everything bought in the shop this run, and what they add up to. The systems they affect
// ask here (Multiplier for percentages, Bonus for amounts) instead of being changed directly, so the
// weapon and enemy data assets are never touched. Resets every run (scene load), like Currency.
public static class Upgrades
{
    private static readonly Dictionary<UpgradeType, int> levels = new();

    public static event Action<UpgradeType> OnUpgraded;

    public static int Level(UpgradeType type) => levels.TryGetValue(type, out int level) ? level : 0;

    // Total added by the levels bought: valuePerLevel x level (a percentage or an amount)
    public static float Bonus(UpgradeType type)
    {
        UpgradeDefinition definition = ShopSettings.Instance != null ? ShopSettings.Instance.Get(type) : null;
        return definition != null ? definition.valuePerLevel * Level(type) : 0f;
    }

    // For percentage upgrades: 1 with none bought, 1.2 after +20%
    public static float Multiplier(UpgradeType type) => 1f + Bonus(type) / 100f;

    public static void Add(UpgradeType type)
    {
        levels[type] = Level(type) + 1;
        OnUpgraded?.Invoke(type);
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        // Static state survives play sessions when domain reload is off, so clear it explicitly
        levels.Clear();
        OnUpgraded = null;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode) => levels.Clear();
}
