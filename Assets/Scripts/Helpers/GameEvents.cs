using System;
using UnityEngine;

public static class GameEvents
{
    public static event Action OnPlayerDied;
    public static event Action OnCastleDied;
    public static event Action OnWaveCleared;
    // A new wave begins (its number, 1 for the first)
    public static event Action<int> OnWaveStarted;

    public static void PlayerDied() => OnPlayerDied?.Invoke();
    public static void CastleDied() => OnCastleDied?.Invoke();
    public static void WaveCleared() => OnWaveCleared?.Invoke();
    public static void WaveStarted(int wave) => OnWaveStarted?.Invoke(wave);
}
