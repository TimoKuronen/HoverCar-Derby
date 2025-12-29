using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// HOST MODE: Game Manager (for client-hosted games, not dedicated servers)
/// 
/// Handles host mode with Unity Lobbies integration:
/// 1. Creates Relay allocation (for NAT traversal)
/// 2. Gets join code from Relay
/// 3. Creates Unity Lobby with join code stored in lobby data
/// 4. Starts host (NetworkManager.StartHost)
/// 5. Loads PlayScene for all connected clients
/// 
/// LOBBY SYSTEM:
/// - Host creates lobby via Unity Lobbies service
/// - Join code stored in lobby data (visible to lobby members)
/// - Clients can browse lobbies or join via code
/// - Lobby heartbeat keeps lobby alive (15s intervals)
/// - When client leaves, removes from lobby
/// 
/// DIFFERS FROM DEDICATED SERVER:
/// - No matchmaking (client-hosted, not Multiplay)
/// - Uses Unity Lobbies (not matchmaker queues)
/// - Host is also a client (IsHost = true)
/// - No backfilling (lobby-based, not matchmaker-based)
/// 
/// USAGE: Called from MainMenu.StartHost() when user clicks "Start Host" button.
/// </summary>
public class HostGameManager : IDisposable
{
    public string joinCode { get; private set; }
    private string lobbyID;
    private Allocation allocation;
    private NetworkObject playerPrefab;
    public NetworkServer NetworkServer { get; private set; }
    private Coroutine heartbeatCoroutine;
    private const int MaxConnections = 8;
    private const string GameSceneName = "PlayScene";

    public HostGameManager(NetworkObject playerPrefab)
    {
        this.playerPrefab = playerPrefab;
    }

    /// <summary>Creates Relay allocation, lobby, starts host, and loads game scene.</summary>
    public async Task StartHostAsync()
    {
        try
        {
            allocation = await Relay.Instance.CreateAllocationAsync(MaxConnections);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start host: {e.Message}");
            return;
        }

        try
        {
            joinCode = await Relay.Instance.GetJoinCodeAsync(allocation.AllocationId);
            Debug.Log($"Join code: {joinCode}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start host: {e.Message}");
            return;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        // Use "dtls" for more security but if facing issues, try "udp"
        RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
        transport.SetRelayServerData(relayServerData);

        try
        {
            CreateLobbyOptions lobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = false,
                Data = new Dictionary<string, DataObject>()
                {
                    {
                    "JoinCode", new DataObject(
                        visibility: DataObject.VisibilityOptions.Member,
                        value: joinCode
                        )
                    }
                }
            };

            string playerName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "Unknown");

            Lobby lobby = await Lobbies.Instance.CreateLobbyAsync(
               $"{playerName}'s Lobby", MaxConnections, lobbyOptions);

            lobbyID = lobby.Id;

            heartbeatCoroutine = HostSingleton.Instance.StartCoroutine(HeartbeatLobby(15));
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to start host: {e.Message}");
            return;
        }

        NetworkServer = new NetworkServer(NetworkManager.Singleton, playerPrefab);

        UserData hostUserData = new UserData
        {
            userName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "!!Missing Name!!"),
            userAuthId = AuthenticationService.Instance.PlayerId
        };

        // Register host manually so GetUserData works later
        NetworkServer.RegisterHostUserData(hostUserData);

        NetworkManager.Singleton.StartHost();

        NetworkServer.OnClientLeft += HandleClientLeft;

        await Task.Yield();

        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
        }
    }

    /// <summary>Removes player from lobby when they disconnect.</summary>
    private async void HandleClientLeft(string authId)
    {
        // Don't try to remove players if we're already shutting down or lobby is gone
        if (string.IsNullOrEmpty(lobbyID))
            return;

        try
        {
            await LobbyService.Instance.RemovePlayerAsync(lobbyID, authId);
        }
        catch (LobbyServiceException e)
        {
            // "lobby not found" is expected during shutdown - don't log as error
            if (e.Message.Contains("not found") || e.Message.Contains("lobby not found"))
            {
                Debug.Log($"[HostGameManager] Lobby already deleted when removing player (expected during shutdown)");
            }
            else
            {
                Debug.LogWarning($"[HostGameManager] Failed to remove player from lobby: {e.Message}");
            }
        }
        catch (System.Exception e)
        {
            // Handle any other exceptions gracefully during shutdown
            Debug.LogWarning($"[HostGameManager] Exception during player removal (expected during shutdown): {e.Message}");
        }
    }

    /// <summary>Keeps lobby alive by sending heartbeat pings at specified interval.</summary>
    private IEnumerator HeartbeatLobby(float waitTimeSeconds)
    {
        WaitForSecondsRealtime delay = new WaitForSecondsRealtime(waitTimeSeconds);

        while (HostSingleton.Instance != null && !string.IsNullOrEmpty(lobbyID))
        {
            try
            {
                Lobbies.Instance.SendHeartbeatPingAsync(lobbyID);
            }
            catch (System.Exception e)
            {
                // Lobby might be deleted or service unavailable during shutdown
                Debug.LogWarning($"[HostGameManager] Heartbeat failed (expected during shutdown): {e.Message}");
                yield break;
            }

            yield return delay;
        }
    }

    /// <summary>Cleans up lobby and network resources.</summary>
    public void Dispose()
    {
        Shutdown();
    }

    /// <summary>Deletes lobby and shuts down network server.</summary>
    public async void Shutdown()
    {
        if (string.IsNullOrEmpty(lobbyID))
            return;

        // Stop heartbeat coroutine if it's running
        if (heartbeatCoroutine != null && HostSingleton.Instance != null)
        {
            HostSingleton.Instance.StopCoroutine(heartbeatCoroutine);
            heartbeatCoroutine = null;
        }

        // Try to delete lobby, but don't error if it's already gone (expected during shutdown)
        try
        {
            await Lobbies.Instance.DeleteLobbyAsync(lobbyID);
        }
        catch (LobbyServiceException e)
        {
            // "lobby not found" is expected during shutdown - don't log as error
            if (e.Message.Contains("not found") || e.Message.Contains("lobby not found"))
            {
                Debug.Log($"[HostGameManager] Lobby already deleted (expected during shutdown)");
            }
            else
            {
                Debug.LogWarning($"[HostGameManager] Failed to delete lobby: {e.Message}");
            }
        }
        catch (System.Exception e)
        {
            // Handle any other exceptions gracefully during shutdown
            Debug.LogWarning($"[HostGameManager] Exception during lobby deletion (expected during shutdown): {e.Message}");
        }
        
        lobbyID = string.Empty;

        // Unsubscribe from events if NetworkServer still exists
        if (NetworkServer != null)
        {
            NetworkServer.OnClientLeft -= HandleClientLeft;
            NetworkServer.Dispose();
        }
    }
}
