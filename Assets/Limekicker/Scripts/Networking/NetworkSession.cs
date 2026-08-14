using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Facade for host/client session actions; UI and gameplay call here instead of singletons.
/// </summary>
public static class NetworkSession
{
    public static bool IsHostActive =>
        NetworkManager.Singleton != null && NetworkManager.Singleton.IsHost;

    public static bool IsClientActive =>
        NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient;

    public static bool IsServerActive =>
        NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer;

    public static bool IsConnected =>
        NetworkManager.Singleton != null &&
        (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer);

    public static bool IsHostInitialized =>
        HostSingleton.Instance != null && HostSingleton.Instance.GameManager != null;

    public static bool IsClientInitialized =>
        ClientSingleton.Instance != null && ClientSingleton.Instance.GameManager != null;

    /// <summary>
    /// Relay allocation, lobby creation, and scene load are handled by HostGameManager.
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
    /// Non-MVP path: dedicated-server matchmaking via ClientGameManager.
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

    public static async Task CancelMatchmakingAsync()
    {
        if (ClientSingleton.Instance == null || ClientSingleton.Instance.GameManager == null)
        {
            return;
        }

        await ClientSingleton.Instance.GameManager.CancelMatchmaking();
    }

    /// <summary>
    /// Host shuts down via HostGameManager; client disconnects via ClientGameManager.
    /// </summary>
    public static void LeaveGame()
    {
        if (IsHostActive && IsHostInitialized)
        {
            HostSingleton.Instance.GameManager.Shutdown();
        }

        if (IsClientInitialized)
        {
            ClientSingleton.Instance.GameManager.Disconnect();
        }
    }

    public static void ReturnToMainMenu() => LeaveGame();

    /// <summary>
    /// Host reloads PlayScene through NGO so clients follow; client loads locally as fallback.
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

    public static INetworkServer GetNetworkServer()
    {
        if (NetworkManager.Singleton == null)
            return null;

        if (NetworkManager.Singleton.IsHost && IsHostInitialized)
        {
            return HostSingleton.Instance.GameManager.NetworkServer;
        }

        if (NetworkManager.Singleton.IsServer &&
            ServerSingleton.Instance != null &&
            ServerSingleton.Instance.GameManager != null)
        {
            return ServerSingleton.Instance.GameManager.NetworkServer;
        }

        return null;
    }

    /// <param name="count">Maximum number of lobbies to return (default: 25)</param>
    /// <exception cref="InvalidOperationException">ClientSingleton is not initialized.</exception>
    public static async Task<QueryResponse> QueryAvailableLobbiesAsync(int count = 25)
    {
        if (!IsClientInitialized)
        {
            throw new InvalidOperationException("Cannot query lobbies: ClientSingleton is not initialized.");
        }

        LobbyService lobbyService = new LobbyService();
        return await lobbyService.QueryAvailableLobbiesAsync(count);
    }

    /// <summary>
    /// Joins a lobby by ID, then connects using the lobby Relay join code.
    /// </summary>
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

    public static string GetHostJoinCode()
    {
        if (IsHostActive && IsHostInitialized)
        {
            return HostSingleton.Instance.GameManager.joinCode;
        }

        return null;
    }

    public static ulong GetLocalClientId()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsClient)
        {
            return NetworkManager.Singleton.LocalClientId;
        }

        return 0;
    }

    public static int GetConnectedClientCount()
    {
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            return NetworkManager.Singleton.ConnectedClientsIds.Count;
        }

        return 0;
    }
}
