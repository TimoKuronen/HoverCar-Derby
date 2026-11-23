using System;
using System.Collections.Generic;

/// <summary>
/// Contract for the session browser view: refresh and join requests out, session rows and busy state in.
/// </summary>
public interface ILobbiesView
{
    event Action OnRefreshRequested;
    event Action<string> OnJoinRequested;

    void ShowSessions(IReadOnlyList<SessionListEntry> sessions);
    void SetBusy(bool busy);
}
