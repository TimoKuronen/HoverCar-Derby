using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
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
    private Allocation allocation;
    private NetworkObject playerPrefab;
    public NetworkServer NetworkServer { get; private set; }
    private LobbyService lobbyService;
    private RelayService relayService;
    private const int MaxConnections = 8;
    private const string GameSceneName = "PlayScene";

    public HostGameManager(NetworkObject playerPrefab, MonoBehaviour coroutineRunner = null)
    {
        this.playerPrefab = playerPrefab;
        this.lobbyService = new LobbyService(coroutineRunner ?? HostSingleton.Instance);
        this.relayService = new RelayService();
    }

    /// <summary>Creates Relay allocation, lobby, starts host, and loads game scene.</summary>
    public async Task StartHostAsync()
    {
        try
        {
            (allocation, joinCode) = await relayService.CreateAllocationWithJoinCodeAsync(MaxConnections);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start host: {e.Message}");
            return;
        }

        try
        {
            relayService.ConfigureTransportForHost(allocation);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to configure transport: {e.Message}");
            return;
        }

        try
        {
            string playerName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "Unknown");
            await lobbyService.CreateLobbyAsync($"{playerName}'s Lobby", MaxConnections, joinCode, isPrivate: false);
            lobbyService.StartHeartbeat(15);
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
        await lobbyService.RemovePlayerAsync(authId);
    }

    /// <summary>Cleans up lobby and network resources.</summary>
    public void Dispose()
    {
        Shutdown();
    }

    /// <summary>Deletes lobby and shuts down network server.</summary>
    public async void Shutdown()
    {
        await lobbyService.DeleteLobbyAsync();

        // Unsubscribe from events if NetworkServer still exists
        if (NetworkServer != null)
        {
            NetworkServer.OnClientLeft -= HandleClientLeft;
            NetworkServer.Dispose();
        }
    }
}
