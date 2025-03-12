using System;
using UnityEngine;

public class GameManager : IGameManager, IDisposable
{
    public bool GameSetupCompleted { get; private set; }

    public event Action VictoryEvent;
    public event Action DefeatEvent;
    public event Action OnGameSetupStarted;
    public event Action GamePaused;
    public event Action GameUnPaused;

    private bool pauseOn;

    public void Initialize()
    {
        Services.Get<IInputManager>().OnCancel += PauseToggle;

        StartGameAfterDelay();
    }

    async void StartGameAfterDelay()
    {
        await MathMethods.WaitForGameTimeAsync(0.5f);
        Debug.Log("game setup completed");
        GameSetupCompleted = true;
    }

    private void RestartGame()
    {
        PauseToggle();
        Loader.Restart();
    }

    private void GameLost()
    {
        DefeatEvent?.Invoke();
        //PauseToggle();
    }

    private void GameWon()
    {
        //VictoryEvent?.Invoke();
        //PauseToggle();
    }

    private void PauseToggle()
    {
        pauseOn = !pauseOn;

        if (pauseOn)
        {
            GamePaused?.Invoke();
        }
        else
        {
            GameUnPaused?.Invoke();
        }
    }

    public void CallRestart()
    {
        RestartGame();
    }

    public void CallWinState()
    {
        GameWon();
    }

    public void CallLoseState()
    {
        GameLost();
    }

    public void Dispose()
    {
        Services.Get<IInputManager>().OnCancel -= PauseToggle;
    }
}