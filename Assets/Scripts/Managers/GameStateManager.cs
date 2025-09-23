using UnityEngine;
using System;

public static class GameStateManager
{

    public static GameState CurrentState { get; private set; } = GameState.Playing;

    public static event Action<GameState> OnGameStateChanged;

    public static void SetState(GameState newState)
    {
        if (CurrentState == newState) return;

        CurrentState = newState;
        OnGameStateChanged?.Invoke(newState);
    }
    public static void ResetState()
    {
        SetState(GameState.Playing);
    }
}
