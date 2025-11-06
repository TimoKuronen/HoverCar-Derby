using System;

public interface IGameManager
{
    event Action OnGameSetupStarted;
    bool GameSetupCompleted { get; }
    event Action VictoryEvent;
    event Action DefeatEvent;
    void CallRestart();
    void CallWinState();
    void CallLoseState();
}
