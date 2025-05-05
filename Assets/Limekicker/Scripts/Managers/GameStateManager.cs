using System;

public class GameStateHandler : IGameStateHandler
{
    private GameState currentGameState;
    public GameState GetCurrentGameState => currentGameState;

    public event Action<GameState> OnGameStateChanged;

    public void Initialize()
    {
        SetGameState(GameState.Normal);
    }

    public void SetGameState(GameState stateToUse)
    {
        currentGameState = stateToUse;

        OnGameStateChanged?.Invoke(stateToUse);
    }
}

public enum GameState
{
    Preparation,
    Normal,
    Paused,
    Win,
    Lose,
}
