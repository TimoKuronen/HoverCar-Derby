using System;

internal class GameManager : IGameManager
{
    public bool GameSetupCompleted => throw new NotImplementedException();

    public event Action OnGameSetupStarted;
    public event Action VictoryEvent;
    public event Action DefeatEvent;

    public GameManager()
    {

    }

    public void CallLoseState()
    {
        throw new NotImplementedException();
    }

    public void CallRestart()
    {
        throw new NotImplementedException();
    }

    public void CallWinState()
    {
        throw new NotImplementedException();
    }
}