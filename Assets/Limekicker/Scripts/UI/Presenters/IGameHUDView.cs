using System;

public interface IGameHUDView
{
    void SetDebugText(string text);
    void ShowPauseMenu(bool show);
    void ShowWinMenu(bool show);

    event Action OnResumeClicked;
    event Action OnRestartClicked;
    event Action OnGoToMenuClicked;
}
