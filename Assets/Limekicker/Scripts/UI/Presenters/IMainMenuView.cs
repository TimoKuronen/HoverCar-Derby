using System;

public interface IMainMenuView
{
    event Action OnHostClicked;
    event Action OnJoinClicked;

    string GetJoinCode();
    void SetBusy(bool busy);
}
