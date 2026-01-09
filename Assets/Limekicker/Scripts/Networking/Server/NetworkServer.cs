using System;
using System.Collections.Generic;
using Unity.Netcode;
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

    /// <summary>Opens server connection. In UNITY_SERVER builds, binds to Multiplay allocated port.</summary>
    public bool OpenConnection(string ip, int port)
    {
        ConnectionService.ConfigureServerBinding(ip, port);
        return networkManager.StartServer();
    }

    /// <summary>Registers host user data and raises OnUserJoined event. Called when host starts.</summary>
    public void RegisterHostUserData(UserData userData)
    {
        ulong hostClientId = NetworkManager.Singleton.LocalClientId;

        clientIdToAuth[hostClientId] = userData.userAuthId;
        authIdToUserData[userData.userAuthId] = userData;

        OnUserJoined?.Invoke(userData);
        //Debug.Log($"[NetworkServer] Registered host user data for client {hostClientId} ({userData.userName})");
    }

    /// <summary>Approves client connections. Extracts UserData from payload and maps clientId to authId.</summary>
    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        if (request.Payload == null || request.Payload.Length == 0)
        {
            //Debug.Log("[NetworkServer] Host approval with no payload - auto-approve self.");
            response.Approved = true;
            response.CreatePlayerObject = false;
            return;
        }

        string payload = System.Text.Encoding.UTF8.GetString(request.Payload);
        UserData userData = JsonUtility.FromJson<UserData>(payload);

        if (userData == null)
        {
            //Debug.LogWarning("[NetworkServer] Invalid userData payload - approving anyway.");
            response.Approved = true;
            response.CreatePlayerObject = false;
            return;
        }

        clientIdToAuth[request.ClientNetworkId] = userData.userAuthId;
        authIdToUserData[userData.userAuthId] = userData;
        //Debug.Log($"[NetworkServer] Approved connection for {userData.userName} (Client ID: {request.ClientNetworkId})");
        response.Approved = true;
        response.CreatePlayerObject = false;
    }

    /// <summary>Sets up disconnect callback when server becomes ready.</summary>
    private void OnNetworkReady()
    {
        networkManager.OnClientDisconnectCallback += OnClientDisconnect;
    }

    /// <summary>Raises OnUserJoined when client fully connects. Called after approval.</summary>
    private void OnClientConnected(ulong clientId)
    {
        if (clientIdToAuth.TryGetValue(clientId, out var authId))
        {
            if (authIdToUserData.TryGetValue(authId, out var userData))
            {
                //Debug.Log($"[NetworkServer] Client connected: {clientId} ({userData.userName}) — raising OnUserJoined");
                OnUserJoined?.Invoke(userData);
            }
        }
    }

    /// <summary>Retrieves UserData for a given clientId.</summary>
    public UserData GetUserData(ulong clientId)
    {
        //Debug.Log($"Getting user data for client ID: {clientId}");
        if (clientIdToAuth.TryGetValue(clientId, out string authId))
        {
            if (authIdToUserData.TryGetValue(authId, out UserData userData))
            {
                return userData;
            }
        }
        //Debug.Log($"User data not found for client ID: {clientId}");
        return null;
    }

    /// <summary>Returns list of all currently connected users.</summary>
    public IReadOnlyList<UserData> GetConnectedUsers()
    {
        var list = new List<UserData>();
        foreach (var authId in clientIdToAuth.Values)
        {
            if (authIdToUserData.TryGetValue(authId, out var user)) list.Add(user);
        }
        return list;
    }

    /// <summary>Maps userAuthId to clientId. Returns false if not found.</summary>
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

    /// <summary>Gets clientId for a UserData object via its authId.</summary>
    public bool TryGetClientIdForUser(UserData userData, out ulong clientId)
    {
        clientId = 0UL;
        if (userData == null || string.IsNullOrEmpty(userData.userAuthId)) return false;
        return TryGetClientIdByAuthId(userData.userAuthId, out clientId);
    }

    /// <summary>Handles client disconnect: raises OnUserLeft and cleans up mappings.</summary>
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
