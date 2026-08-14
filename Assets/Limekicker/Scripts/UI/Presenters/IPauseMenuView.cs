using System;

/// <summary>
/// Contract for pause menu visibility and player actions.
/// </summary>
public interface IPauseMenuView
{
    void ShowPauseMenu(bool show);

    event Action OnPauseMenuClicked;
    event Action OnResumeClicked;
    event Action OnGoToMenuClicked;
}
