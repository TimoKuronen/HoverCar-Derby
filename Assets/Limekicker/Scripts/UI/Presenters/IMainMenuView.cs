using System;

/// <summary>
/// Contract for main menu view host/join actions and busy state.
/// </summary>
public interface IMainMenuView
{
    event Action OnHostClicked;
    event Action OnJoinClicked;

    string GetJoinCode();
    void SetBusy(bool busy);
}
