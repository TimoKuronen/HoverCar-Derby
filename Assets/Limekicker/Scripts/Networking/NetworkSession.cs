using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Central entry point for high-level network session actions.
///
/// UI and gameplay code should call into this class instead of talking
/// directly to HostSingleton / ClientSingleton / ServerSingleton / Lobbies.
/// This keeps the overall flow in one place while still reusing the existing
/// HostGameManager / ClientGameManager / ServerGameManager implementations.
/// 
/// This facade completely hides singleton access from the rest of the codebase.
/// </summary>
public static class NetworkSession
{
    // Connection Status Properties
    /// <summary>Returns true if a NetworkManager exists and is acting as host.</summary>
    public static bool IsHostActive =>
        NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

    /// <summary>Returns true if a NetworkManager exists and is acting as a client.</summary>
    public static bool IsClientActive =>
        NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient;

    /// <summary>Returns true if a NetworkManager exists and is acting as a server (host or dedicated).</summary>
    public static bool IsServerActive =>
        NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

    /// <summary>Returns true if currently connected to a network session.</summary>
    public static bool IsConnected =>
        NetworkManager.Singleton != null && 
        (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer);

    /// <summary>Returns true if HostSingleton is initialized and ready.</summary>
    public static bool IsHostInitialized =>
        HostSingleton.Instance != null && HostSingleton.Instance.GameManager != null;

    /// <summary>Returns true if ClientSingleton is initialized and ready.</summary>
    public static bool IsClientInitialized =>
        ClientSingleton.Instance != null && ClientSingleton.Instance.GameManager != null;

    /// <summary>
    /// Starts a host game using the existing HostGameManager.
    /// Relay allocation, lobby creation, and scene loading are handled there.
    /// </summary>
    public static async Task StartHostAsync()
    {
        if (HostSingleton.Instance == null || HostSingleton.Instance.GameManager == null)
        {
            SessionNotifications.Error(
                "Could not start host.",
                "[NetworkSession] Cannot start host: HostSingleton or GameManager is missing.");
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
            SessionNotifications.Error(
                "Could not start client.",
                "[NetworkSession] Cannot start client: ClientSingleton or GameManager is missing.");
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
            SessionNotifications.Error(
                "Matchmaking is unavailable.",
                "[NetworkSession] Cannot start matchmaking: ClientSingleton or GameManager is missing.");
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
        if (IsHostActive && IsHostInitialized)
        {
            HostSingleton.Instance.GameManager.Shutdown();
        }

        // Client path: disconnect from server
        if (IsClientInitialized)
        {
            ClientSingleton.Instance.GameManager.Disconnect();
        }
    }

    /// <summary>
    /// Leaves the current session and returns to MainMenu via LeaveGame.
    /// </summary>
    public static void ReturnToMainMenu() => LeaveGame();

    /// <summary>
    /// Reloads the current PlayScene for a rematch.
    /// Host uses NGO scene manager so clients sync; client falls back to local load.
    /// </summary>
    public static void RestartCurrentMatch()
    {
        if (IsHostActive && NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(
                SceneManager.GetActiveScene().name,
                LoadSceneMode.Single);
            return;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    // NetworkServer Access
    /// <summary>
    /// Gets the NetworkServer instance for the current session.
    /// Returns null if not available (client-only mode or not initialized).
    /// </summary>
    public static INetworkServer GetNetworkServer()
    {
        if (NetworkManager.Singleton == null) return null;

        // Host path
        if (NetworkManager.Singleton.IsHost && IsHostInitialized)
        {
            return HostSingleton.Instance.GameManager.NetworkServer;
        }

        // Dedicated server path
        if (NetworkManager.Singleton.IsServer && 
            ServerSingleton.Instance != null && 
            ServerSingleton.Instance.GameManager != null)
        {
            return ServerSingleton.Instance.GameManager.NetworkServer;
        }

        return null;
    }

    // Lobby Operations
    /// <summary>
    /// Queries available lobbies with default filters (has available slots, not locked).
    /// </summary>
    /// <param name="count">Maximum number of lobbies to return (default: 25)</param>
    /// <returns>Query response with matching lobbies. Throws exception on error.</returns>
    public static async Task<QueryResponse> QueryAvailableLobbiesAsync(int count = 25)
    {
        if (!IsClientInitialized)
        {
            throw new InvalidOperationException("Cannot query lobbies: ClientSingleton is not initialized.");
        }

        // Create a temporary LobbyService for querying (UI classes can also use this)
        LobbyService lobbyService = new LobbyService();
        return await lobbyService.QueryAvailableLobbiesAsync(count);
    }

    /// <summary>
    /// Joins a lobby by its ID and then connects to the game via the lobby's join code.
    /// </summary>
    /// <param name="lobbyId">The ID of the lobby to join</param>
    public static async Task JoinLobbyByIdAsync(string lobbyId)
    {
        if (!IsClientInitialized)
        {
            SessionNotifications.Error(
                "Could not join lobby.",
                "[NetworkSession] Cannot join lobby: ClientSingleton is not initialized.");
            return;
        }

        if (string.IsNullOrWhiteSpace(lobbyId))
        {
            SessionNotifications.Error(
                "Could not join lobby.",
                "[NetworkSession] Cannot join lobby: lobby ID is null or empty.");
            return;
        }

        try
        {
            LobbyService lobbyService = new LobbyService();
            Lobby lobby = await lobbyService.JoinLobbyByIdAsync(lobbyId);

            if (lobby.Data != null && lobby.Data.ContainsKey("JoinCode"))
            {
                string joinCode = lobby.Data["JoinCode"].Value;
                await StartClientViaJoinCodeAsync(joinCode);
            }
            else
            {
                SessionNotifications.Error(
                    "Lobby is missing a join code.",
                    "[NetworkSession] Lobby does not contain a join code.");
            }
        }
        catch (Exception e)
        {
            SessionNotifications.Error(
                "Could not join lobby.",
                $"[NetworkSession] Failed to join lobby: {e.Message}");
        }
    }

    /// <summary>
    /// Gets the current join code if hosting.
    /// </summary>
    /// <returns>The join code, or null if not hosting or not available</returns>
    public static string GetHostJoinCode()
    {
        if (IsHostActive && IsHostInitialized)
        {
            return HostSingleton.Instance.GameManager.joinCode;
        }
        return null;
    }

    // Connection Info
    /// <summary>
    /// Gets the local client ID if connected as a client.
    /// </summary>
    /// <returns>The local client ID, or 0 if not connected</returns>
    public static ulong GetLocalClientId()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            return NetworkManager.Singleton.LocalClientId;
        }
        return 0;
    }

    /// <summary>
    /// Gets the number of connected clients (including host if in host mode).
    /// </summary>
    /// <returns>The number of connected clients, or 0 if not a server</returns>
    public static int GetConnectedClientCount()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            return NetworkManager.Singleton.ConnectedClientsIds.Count;
        }
        return 0;
    }
}


