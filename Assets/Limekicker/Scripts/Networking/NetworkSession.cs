using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Multiplayer;
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
    /// Session creation (lobby and Relay), host start, and scene load are handled by HostGameManager.
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

    public static async Task<bool> StartClientViaJoinCodeAsync(string joinCode)
    {
        if (ClientSingleton.Instance == null || ClientSingleton.Instance.GameManager == null)
        {
            SessionNotifications.Error(
                "Could not start client.",
                "[NetworkSession] Cannot start client: ClientSingleton or GameManager is missing.");
            return false;
        }

        return await ClientSingleton.Instance.GameManager.StartClientAsync(joinCode);
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
            DespawnAllCars();
            NetworkManager.Singleton.SceneManager.LoadScene(
                SceneManager.GetActiveScene().name,
                LoadSceneMode.Single);
            return;
        }

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Player objects survive NGO scene loads while bots do not, so a rematch would otherwise keep stale
    /// player cars and skip respawning them. Despawning every car lets the reloaded scene spawn a clean set.
    /// </summary>
    private static void DespawnAllCars()
    {
        var cars = new List<NetworkObject>();
        foreach (NetworkObject spawned in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            if (spawned != null && spawned.TryGetComponent<PlayerController>(out _))
            {
                cars.Add(spawned);
            }
        }

        foreach (NetworkObject car in cars)
        {
            car.Despawn(true);
        }
    }

    public static INetworkServer GetNetworkServer()
    {
        if (NetworkManager.Singleton == null)
            return null;

        if (NetworkManager.Singleton.IsHost && IsHostInitialized)
        {
            return HostSingleton.Instance.GameManager.NetworkServer;
        }

        return null;
    }

    /// <summary>Public sessions that are not full and not locked.</summary>
    /// <param name="count">Maximum number of sessions to return (default: 25)</param>
    /// <exception cref="InvalidOperationException">ClientSingleton is not initialized.</exception>
    public static Task<IReadOnlyList<ISessionInfo>> QueryAvailableSessionsAsync(int count = 25)
    {
        if (!IsClientInitialized)
        {
            throw new InvalidOperationException("Cannot query sessions: ClientSingleton is not initialized.");
        }

        return ClientSingleton.Instance.GameManager.QueryAvailableSessionsAsync(count);
    }

    /// <summary>Joins a session listed by <see cref="QueryAvailableSessionsAsync"/>.</summary>
    public static async Task<bool> JoinSessionByIdAsync(string sessionId)
    {
        if (!IsClientInitialized)
        {
            SessionNotifications.Error(
                "Could not join lobby.",
                "[NetworkSession] Cannot join session: ClientSingleton is not initialized.");
            return false;
        }

        if (string.IsNullOrWhiteSpace(sessionId))
        {
            SessionNotifications.Error(
                "Could not join lobby.",
                "[NetworkSession] Cannot join session: session ID is null or empty.");
            return false;
        }

        return await ClientSingleton.Instance.GameManager.StartClientByIdAsync(sessionId);
    }

    /// <summary>Host only: stops new players from joining the session.</summary>
    public static Task LockSessionAsync()
    {
        return IsHostActive && IsHostInitialized
            ? HostSingleton.Instance.GameManager.LockSessionAsync()
            : Task.CompletedTask;
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
