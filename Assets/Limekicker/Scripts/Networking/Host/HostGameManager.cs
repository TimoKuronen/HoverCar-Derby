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

        UserData userData = new UserData
        {
            userName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "!!Missing Name!!"),
            userAuthId = AuthenticationService.Instance.PlayerId
        };

        string payload = JsonUtility.ToJson(userData);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

        NetworkManager.Singleton.StartHost();

        NetworkServer.OnClientLeft += HandleClientLeft;

        NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
    }

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

    private IEnumerator HeartbeatLobby(float waitTimeSeconds)
    {
        WaitForSecondsRealtime delay = new WaitForSecondsRealtime(waitTimeSeconds);

        while (true)
        {
            Lobbies.Instance.SendHeartbeatPingAsync(lobbyID);

            yield return delay;
        }
    }

    public void Dispose()
    {
        Shutdown();
    }

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
