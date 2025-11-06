using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetworkServer : INetworkServer, IDisposable
{
    private NetworkManager networkManager;

    public NetworkObject PlayerPrefab { get; private set; }

    public event Action<UserData> OnUserJoined;
    public event Action<UserData> OnUserLeft;
    public Action<string> OnClientLeft;

    private Dictionary<ulong, string> clientIdToAuth = new Dictionary<ulong, string>();
    private Dictionary<string, UserData> authIdToUserData = new Dictionary<string, UserData>();

    public NetworkServer(NetworkManager networkManager, NetworkObject playerPrefab)
    {
        this.networkManager = networkManager;
        this.PlayerPrefab = playerPrefab;

        networkManager.ConnectionApprovalCallback += ApprovalCheck;
        networkManager.OnServerStarted += OnNetworkReady;
        networkManager.OnClientConnectedCallback += OnClientConnected;
    }

    public bool OpenConnection(string ip, int port)
    {
        UnityTransport transport = networkManager.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, (ushort)port);
        return networkManager.StartServer();
    }

    public void RegisterHostUserData(UserData userData)
    {
        ulong hostClientId = NetworkManager.Singleton.LocalClientId;

        clientIdToAuth[hostClientId] = userData.userAuthId;
        authIdToUserData[userData.userAuthId] = userData;

        OnUserJoined?.Invoke(userData);
        Debug.Log($"[NetworkServer] Registered host user data for client {hostClientId} ({userData.userName})");
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        if (request.Payload == null || request.Payload.Length == 0)
        {
            Debug.Log("[NetworkServer] Host approval with no payload � auto-approve self.");
            response.Approved = true;
            response.CreatePlayerObject = false;
            return;
        }

        string payload = System.Text.Encoding.UTF8.GetString(request.Payload);
        UserData userData = JsonUtility.FromJson<UserData>(payload);

        if (userData == null)
        {
            Debug.LogWarning("[NetworkServer] Invalid userData payload � approving anyway.");
            response.Approved = true;
            response.CreatePlayerObject = false;
            return;
        }

        clientIdToAuth[request.ClientNetworkId] = userData.userAuthId;
        authIdToUserData[userData.userAuthId] = userData;
        Debug.Log($"[NetworkServer] Approved connection for {userData.userName} (Client ID: {request.ClientNetworkId})");
        response.Approved = true;
        response.CreatePlayerObject = false;
    }

    private void OnNetworkReady()
    {
        networkManager.OnClientDisconnectCallback += OnClientDisconnect;
    }

    private void OnClientConnected(ulong clientId)
    {
        if (clientIdToAuth.TryGetValue(clientId, out var authId))
        {
            if (authIdToUserData.TryGetValue(authId, out var userData))
            {
                Debug.Log($"[NetworkServer] Client connected: {clientId} ({userData.userName}) — raising OnUserJoined");
                OnUserJoined?.Invoke(userData);
            }
        }
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

    public IReadOnlyList<UserData> GetConnectedUsers()
    {
        var list = new List<UserData>();
        foreach (var authId in clientIdToAuth.Values)
        {
            if (authIdToUserData.TryGetValue(authId, out var user)) list.Add(user);
        }
        return list;
    }

    public bool TryGetClientIdByAuthId(string userAuthId, out ulong clientId)
    {
        foreach (var kvp in clientIdToAuth)
        {
            if (kvp.Value == userAuthId)
            {
                clientId = kvp.Key;
                return true;
            }
        }
        clientId = 0UL;
        return false;
    }

    public bool TryGetClientIdForUser(UserData userData, out ulong clientId)
    {
        clientId = 0UL;
        if (userData == null || string.IsNullOrEmpty(userData.userAuthId)) return false;
        return TryGetClientIdByAuthId(userData.userAuthId, out clientId);
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
        networkManager.OnClientConnectedCallback -= OnClientConnected;
        networkManager.OnClientDisconnectCallback -= OnClientDisconnect;

        if (networkManager.IsListening)
        {
            networkManager.Shutdown();
        }
    }
}
