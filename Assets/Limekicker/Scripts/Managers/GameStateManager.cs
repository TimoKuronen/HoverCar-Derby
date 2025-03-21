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

    private void TogglePause()
    {
        if (currentGameState == GameState.Normal)
        {
            SetGameState(GameState.Paused);
        }
        else if (currentGameState == GameState.Paused)
        {
            SetGameState(GameState.Normal);
        }
    }

    public void SetGameState(GameState stateToUse)
    {
        currentGameState = stateToUse;

        OnGameStateChanged?.Invoke(stateToUse);
    }
}

public enum GameState
{
    Normal,
    Paused,
    LevelUp,
    Lost
}
