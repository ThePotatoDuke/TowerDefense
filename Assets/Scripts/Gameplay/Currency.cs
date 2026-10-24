using System;
using UnityEngine;
using UnityEngine.SceneManagement;

// Grass blades the player has collected, to spend on upgrades. Resets every run (scene load).
public static class Currency
{
    public static int Grass { get; private set; }

    public static event Action<int> OnGrassChanged;

    public static void AddGrass(int amount)
    {
        if (amount <= 0) return;
        Grass += amount;
        OnGrassChanged?.Invoke(Grass);
    }

    public static bool TrySpendGrass(int amount)
    {
        if (amount < 0 || Grass < amount) return false;
        Grass -= amount;
        OnGrassChanged?.Invoke(Grass);
        return true;
    }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    private static void Initialize()
    {
        // Static state survives play sessions when domain reload is off, so clear it explicitly
        Grass = 0;
        OnGrassChanged = null;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Grass = 0;
        OnGrassChanged?.Invoke(Grass);
    }
}
