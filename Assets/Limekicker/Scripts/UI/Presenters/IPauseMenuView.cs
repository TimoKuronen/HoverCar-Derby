using System;

public interface IPauseMenuView
{
    void ShowPauseMenu(bool show);

    event Action OnPauseMenuClicked;
    event Action OnResumeClicked;
    event Action OnGoToMenuClicked;
}
