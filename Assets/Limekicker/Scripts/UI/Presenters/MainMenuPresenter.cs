using System;

public class MainMenuPresenter : BasePresenter
{
    private readonly IMainMenuView view;
    private bool isBusy;

    public MainMenuPresenter(IMainMenuView view)
    {
        this.view = view;
    }

    protected override void SubscribeToModels()
    {
        view.OnHostClicked += HandleHostClicked;
        view.OnJoinClicked += HandleJoinClicked;
    }

    protected override void UnsubscribeFromModels()
    {
        view.OnHostClicked -= HandleHostClicked;
        view.OnJoinClicked -= HandleJoinClicked;
    }

    private async void HandleHostClicked()
    {
        if (isBusy)
            return;

        isBusy = true;
        view.SetBusy(true);

        try
        {
            SessionNotifications.Info("Starting host...");
            await NetworkSession.StartHostAsync();
        }
        finally
        {
            isBusy = false;
            view.SetBusy(false);
        }
    }

    private async void HandleJoinClicked()
    {
        if (isBusy)
            return;

        string joinCode = view.GetJoinCode()?.Trim().ToUpperInvariant();
        if (string.IsNullOrEmpty(joinCode))
        {
            SessionNotifications.Warn("Enter a join code from the host.");
            return;
        }

        isBusy = true;
        view.SetBusy(true);

        try
        {
            SessionNotifications.Info("Joining game...");
            await NetworkSession.StartClientViaJoinCodeAsync(joinCode);
        }
        finally
        {
            isBusy = false;
            view.SetBusy(false);
        }
    }
}
