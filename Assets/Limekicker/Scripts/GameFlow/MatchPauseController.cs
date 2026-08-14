using System;

/// <summary>
/// Local pause overlay on top of match phases. Stores the state to restore when unpausing.
/// </summary>
public class MatchPauseController
{
    private readonly Func<IGameState> getCurrentState;
    private readonly Action<IGameState> changeState;

    private IGameState stateBeforePause;

    public MatchPauseController(Func<IGameState> getCurrentState, Action<IGameState> changeState)
    {
        this.getCurrentState = getCurrentState;
        this.changeState = changeState;
    }

    public bool CanPause => getCurrentState() is PlayState;

    public bool IsPaused => getCurrentState() is PauseState;

    public void TogglePause()
    {
        if (IsPaused)
        {
            Resume();
            return;
        }

        if (!CanPause)
            return;

        stateBeforePause = getCurrentState();
        changeState(new PauseState());
    }

    public bool TryResumeFromPause()
    {
        if (!IsPaused || stateBeforePause == null)
            return false;

        Resume();
        return true;
    }

    private void Resume()
    {
        IGameState restore = stateBeforePause;
        stateBeforePause = null;
        changeState(restore);
    }
}
