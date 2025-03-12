using System;

public interface IGameStateHandler : IService
{
    GameState GetCurrentGameState { get; }

    void SetGameState(GameState stateToUse);

    event Action<GameState> OnGameStateChanged;
}
