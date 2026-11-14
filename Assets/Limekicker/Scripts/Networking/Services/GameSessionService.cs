using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Multiplayer;
using UnityEngine;

/// <summary>
/// Wraps the Multiplayer Services session API. A session bundles the lobby entry and the Relay allocation,
/// and the SDK starts or stops the NetworkManager as the session is created, joined or left.
/// </summary>
public class GameSessionService
{
    public ISession CurrentSession { get; private set; }

    /// <summary>Creates a public Relay session. The SDK starts the NetworkManager as host before the task completes.</summary>
    public async Task<ISession> CreateHostSessionAsync(string sessionName, int maxPlayers)
    {
        SessionOptions options = new SessionOptions
        {
            Name = sessionName,
            MaxPlayers = maxPlayers,
            IsPrivate = false
        }.WithRelayNetwork();

        CurrentSession = await MultiplayerService.Instance.CreateSessionAsync(options);
        return CurrentSession;
    }

    /// <summary>Joins by session code. The SDK starts the NetworkManager as client before the task completes.</summary>
    public async Task<ISession> JoinByCodeAsync(string sessionCode)
    {
        CurrentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(sessionCode);
        return CurrentSession;
    }

    /// <summary>Joins a session found through <see cref="QueryAvailableAsync"/>.</summary>
    public async Task<ISession> JoinByIdAsync(string sessionId)
    {
        CurrentSession = await MultiplayerService.Instance.JoinSessionByIdAsync(sessionId);
        return CurrentSession;
    }

    /// <summary>Lists public sessions that are not full and not locked.</summary>
    public async Task<IReadOnlyList<ISessionInfo>> QueryAvailableAsync(int count)
    {
        QuerySessionsResults results = await MultiplayerService.Instance.QuerySessionsAsync(
            new QuerySessionsOptions { Count = count });

        List<ISessionInfo> available = new List<ISessionInfo>();
        foreach (ISessionInfo info in results.Sessions)
        {
            if (!info.IsLocked && info.AvailableSlots > 0)
            {
                available.Add(info);
            }
        }

        return available;
    }

    /// <summary>Stops new players from joining. Host only.</summary>
    public async Task LockAsync()
    {
        if (CurrentSession == null || !CurrentSession.IsHost || CurrentSession.IsLocked)
            return;

        try
        {
            IHostSession hostSession = CurrentSession.AsHost();
            hostSession.IsLocked = true;
            await hostSession.SavePropertiesAsync();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GameSessionService] Failed to lock session: {e.Message}");
        }
    }

    /// <summary>Host deletes the session for everyone; a client leaves it. Failures are expected when the session is already gone.</summary>
    public async Task CloseAsync()
    {
        ISession session = CurrentSession;
        CurrentSession = null;

        if (session == null)
            return;

        try
        {
            if (session.IsHost)
            {
                await session.AsHost().DeleteAsync();
            }
            else
            {
                await session.LeaveAsync();
            }
        }
        catch (Exception e)
        {
            Debug.Log($"[GameSessionService] Session already closed: {e.Message}");
        }
    }
}
