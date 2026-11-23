using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Multiplayer;

/// <summary>
/// Drives the session browser: queries open sessions and joins the one the player picks.
/// The session query is rate limited, so the view stays busy for a cooldown after each refresh.
/// </summary>
public class LobbiesPresenter : BasePresenter
{
    private const int MaxSessions = 25;
    private const int RefreshCooldownMs = 3000;

    private readonly ILobbiesView view;
    private bool isBusy;
    private bool isDisposed;

    public LobbiesPresenter(ILobbiesView view)
    {
        this.view = view;
    }

    protected override void SubscribeToModels()
    {
        view.OnRefreshRequested += HandleRefreshRequested;
        view.OnJoinRequested += HandleJoinRequested;
    }

    protected override void UnsubscribeFromModels()
    {
        view.OnRefreshRequested -= HandleRefreshRequested;
        view.OnJoinRequested -= HandleJoinRequested;
    }

    public override void Dispose()
    {
        isDisposed = true;
        base.Dispose();
    }

    private async void HandleRefreshRequested()
    {
        if (isBusy)
            return;

        SetBusy(true);

        try
        {
            await LoadSessionsAsync();
            await Task.Delay(RefreshCooldownMs);
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async void HandleJoinRequested(string sessionId)
    {
        if (isBusy)
            return;

        SetBusy(true);

        try
        {
            SessionNotifications.Info("Joining game...");
            bool joined = await NetworkSession.JoinSessionByIdAsync(sessionId);

            if (!joined)
            {
                await LoadSessionsAsync();
            }
        }
        finally
        {
            SetBusy(false);
        }
    }

    private async Task LoadSessionsAsync()
    {
        try
        {
            IReadOnlyList<ISessionInfo> sessions = await NetworkSession.QueryAvailableSessionsAsync(MaxSessions);
            if (isDisposed)
                return;

            List<SessionListEntry> entries = new List<SessionListEntry>(sessions.Count);
            foreach (ISessionInfo session in sessions)
            {
                entries.Add(new SessionListEntry(
                    session.Id,
                    session.Name,
                    session.MaxPlayers - session.AvailableSlots,
                    session.MaxPlayers));
            }

            view.ShowSessions(entries);
        }
        catch (Exception e)
        {
            SessionNotifications.Error(
                "Could not load sessions.",
                $"[LobbiesPresenter] Failed to query sessions: {e.Message}");
        }
    }

    private void SetBusy(bool busy)
    {
        isBusy = busy;

        if (!isDisposed)
        {
            view.SetBusy(busy);
        }
    }
}
