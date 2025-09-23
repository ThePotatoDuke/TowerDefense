using System;
using UnityEngine;

public static class GameEvents
{
    public static event Action OnPlayerDied;
    public static event Action OnCastleDied;

    public static void PlayerDied() => OnPlayerDied?.Invoke();
    public static void CastleDied() => OnCastleDied?.Invoke();
}
