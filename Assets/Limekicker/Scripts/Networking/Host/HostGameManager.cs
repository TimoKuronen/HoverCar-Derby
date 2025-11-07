using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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
    private string joinCode;
    private string lobbyID;
    private Allocation allocation;
    private NetworkObject playerPrefab;
    public NetworkServer NetworkServer { get; private set; }
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

            HostSingleton.Instance.StartCoroutine(HeartbeatLobby(15));
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
        try
        {
            await LobbyService.Instance.RemovePlayerAsync(lobbyID, authId);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to remove player from lobby: {e.Message}");
        }
    }

    /// <summary>Keeps lobby alive by sending heartbeat pings at specified interval.</summary>
    private IEnumerator HeartbeatLobby(float waitTimeSeconds)
    {
        WaitForSecondsRealtime delay = new WaitForSecondsRealtime(waitTimeSeconds);

        while (true)
        {
            Lobbies.Instance.SendHeartbeatPingAsync(lobbyID);

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

        HostSingleton.Instance.StopCoroutine(nameof(HeartbeatLobby));

        try
        {
            await Lobbies.Instance.DeleteLobbyAsync(lobbyID);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to delete lobby: {e.Message}");
        }
        lobbyID = string.Empty;

        NetworkServer.OnClientLeft -= HandleClientLeft;

        NetworkServer?.Dispose();
    }
}
