using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetworkServer : IDisposable
{
    private NetworkManager networkManager;

    private NetworkObject playerPrefab;

    public Action<UserData> OnUserJoined;
    public Action<UserData> OnUserLeft;
    public Action<string> OnClientLeft;

    private Dictionary<ulong, string> clientIdToAuth = new Dictionary<ulong, string>();
    private Dictionary<string, UserData> authIdToUserData = new Dictionary<string, UserData>();

    public NetworkServer(NetworkManager networkManager, NetworkObject playerPrefab)
    {
        this.networkManager = networkManager;
        this.playerPrefab = playerPrefab;

        networkManager.ConnectionApprovalCallback += ApprovalCheck;
        networkManager.OnServerStarted += OnNetworkReady;
    }

    public bool OpenConnection(string ip, int port)
    {
        UnityTransport transport = networkManager.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, (ushort)port);
        return networkManager.StartServer();
    }

    public void RegisterHostUserData(UserData userData)
    {
        ulong hostClientId = NetworkManager.Singleton.LocalClientId; // typically 0

        clientIdToAuth[hostClientId] = userData.userAuthId;
        authIdToUserData[userData.userAuthId] = userData;

        Debug.Log($"[NetworkServer] Registered host user data for client {hostClientId} ({userData.userName})");
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        if (request.Payload == null || request.Payload.Length == 0)
        {
            Debug.Log("[NetworkServer] Host approval with no payload – auto-approve self.");
            response.Approved = true;
            response.CreatePlayerObject = false;
            return;
        }

        string payload = System.Text.Encoding.UTF8.GetString(request.Payload);
        UserData userData = JsonUtility.FromJson<UserData>(payload);

        if (userData == null)
        {
            Debug.LogWarning("[NetworkServer] Invalid userData payload – approving anyway.");
            response.Approved = true;
            response.CreatePlayerObject = false;
            return;
        }

        clientIdToAuth[request.ClientNetworkId] = userData.userAuthId;
        authIdToUserData[userData.userAuthId] = userData;
        OnUserJoined?.Invoke(userData);

        _ = SpawnPlayerDelay(request.ClientNetworkId);

        response.Approved = true;
        response.CreatePlayerObject = false;
    }

    private async Task SpawnPlayerDelay(ulong clientId)
    {
        await Task.Delay(1000);
      
        NetworkObject playerInstance = GameObject.Instantiate(playerPrefab, SpawnPoint.GetRandomSpawnPos().Item1, Quaternion.identity);

        playerInstance.SpawnAsPlayerObject(clientId);
    }

    private void OnNetworkReady()
    {
        networkManager.OnClientDisconnectCallback += OnClientDisconnect;
    }

    public UserData GetUserData(ulong clientId)
    {
        Debug.Log($"Getting user data for client ID: {clientId}");
        if (clientIdToAuth.TryGetValue(clientId, out string authId))
        {
            if (authIdToUserData.TryGetValue(authId, out UserData userData))
            {
                return userData;
            }
        }
        Debug.Log($"User data not found for client ID: {clientId}");
        return null;
    }

    private void OnClientDisconnect(ulong clientId)
    {
        if (clientIdToAuth.TryGetValue(clientId, out string authId))
        {
            OnUserLeft?.Invoke(authIdToUserData[authId]);
            clientIdToAuth.Remove(clientId);
            authIdToUserData.Remove(authId);
            OnClientLeft?.Invoke(authId);
        }
    }

    public void Dispose()
    {
        if (networkManager == null)
            return;


        networkManager.ConnectionApprovalCallback -= ApprovalCheck;
        networkManager.OnServerStarted -= OnNetworkReady;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnect;

        if (networkManager.IsListening)
        {
            networkManager.Shutdown();
        }

    }
}
