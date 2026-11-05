using System;
using UnityEngine;

public static class GameEvents
{
    public static event Action OnPlayerDied;
    // The hit that kills the player, the moment it lands (OnPlayerDied comes after their death plays out)
    public static event Action OnPlayerFatallyHit;
    public static event Action OnCastleDied;
    public static event Action OnWaveCleared;
    // A new wave begins (its number, 1 for the first)
    public static event Action<int> OnWaveStarted;
    // A boss wave begins (before its banner), and later ends (it's cleared)
    public static event Action<BossWave> OnBossWaveStarted;
    public static event Action OnBossWaveEnded;
    // A boss wave's banner has gone and its enemies are starting
    public static event Action OnBossBannerEnded;
    // The wave just cleared opens the shop (before its clear sound, so the music can hush first)
    public static event Action OnShopComing;

    public static void PlayerDied() => OnPlayerDied?.Invoke();
    public static void PlayerFatallyHit() => OnPlayerFatallyHit?.Invoke();
    public static void CastleDied() => OnCastleDied?.Invoke();
    public static void WaveCleared() => OnWaveCleared?.Invoke();
    public static void WaveStarted(int wave) => OnWaveStarted?.Invoke(wave);
    public static void BossWaveStarted(BossWave wave) => OnBossWaveStarted?.Invoke(wave);
    public static void BossWaveEnded() => OnBossWaveEnded?.Invoke();
    public static void BossBannerEnded() => OnBossBannerEnded?.Invoke();
    public static void ShopComing() => OnShopComing?.Invoke();
}
