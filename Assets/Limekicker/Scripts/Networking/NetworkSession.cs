using System;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Central entry point for high-level network session actions.
///
/// UI and gameplay code should call into this class instead of talking
/// directly to HostSingleton / ClientSingleton / Lobbies where possible.
/// This keeps the overall flow in one place while still reusing the existing
/// HostGameManager / ClientGameManager / ServerGameManager implementations.
/// </summary>
public static class NetworkSession
{
    /// <summary>Returns true if a NetworkManager exists and is acting as host.</summary>
    public static bool IsHostActive =>
        NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

    /// <summary>Returns true if a NetworkManager exists and is acting as a client.</summary>
    public static bool IsClientActive =>
        NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient;

    /// <summary>
    /// Starts a host game using the existing HostGameManager.
    /// Relay allocation, lobby creation, and scene loading are handled there.
    /// </summary>
    public static async Task StartHostAsync()
    {
        if (HostSingleton.Instance == null || HostSingleton.Instance.GameManager == null)
        {
            Debug.LogError("[NetworkSession] Cannot start host: HostSingleton or GameManager is missing.");
            return;
        }

        await HostSingleton.Instance.GameManager.StartHostAsync();
    }

    /// <summary>
    /// Starts a client connection using a Relay join code via ClientGameManager.
    /// </summary>
    public static async Task StartClientViaJoinCodeAsync(string joinCode)
    {
        if (ClientSingleton.Instance == null || ClientSingleton.Instance.GameManager == null)
        {
            Debug.LogError("[NetworkSession] Cannot start client: ClientSingleton or GameManager is missing.");
            return;
        }

        await ClientSingleton.Instance.GameManager.StartClientAsync(joinCode);
    }

    /// <summary>
    /// Starts client-side matchmaking against dedicated servers.
    /// Result is returned via callback once the matchmaker responds.
    /// </summary>
    public static void FindMatchAsync(Action<MatchmakerPollingResult> onMatchmakeResponse)
    {
        if (ClientSingleton.Instance == null || ClientSingleton.Instance.GameManager == null)
        {
            Debug.LogError("[NetworkSession] Cannot start matchmaking: ClientSingleton or GameManager is missing.");
            onMatchmakeResponse?.Invoke(MatchmakerPollingResult.TicketCreationError);
            return;
        }

        ClientSingleton.Instance.GameManager.MatchmakeAsync(onMatchmakeResponse);
    }

    /// <summary>Cancels any active matchmaking ticket on the client.</summary>
    public static async Task CancelMatchmakingAsync()
    {
        if (ClientSingleton.Instance == null || ClientSingleton.Instance.GameManager == null)
        {
            return;
        }

        await ClientSingleton.Instance.GameManager.CancelMatchmaking();
    }

    /// <summary>
    /// Leaves the current game session:
    /// - On host: shuts down HostGameManager (lobby, server, etc.)
    /// - On client: disconnects via ClientGameManager.
    /// </summary>
    public static void LeaveGame()
    {
        // Host path: shut down lobby/host if present
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost &&
            HostSingleton.Instance != null && HostSingleton.Instance.GameManager != null)
        {
            HostSingleton.Instance.GameManager.Shutdown();
        }

        // Client path: disconnect from server
        if (ClientSingleton.Instance != null && ClientSingleton.Instance.GameManager != null)
        {
            ClientSingleton.Instance.GameManager.Disconnect();
        }
    }
}