using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class NetworkServer : IDisposable
{
    private NetworkManager networkManager;

    public Action<UserData> OnUserJoined;
    public Action<UserData> OnUserLeft;
    public Action<string> OnClientLeft;

    private Dictionary<ulong, string> clientIdToAuth = new Dictionary<ulong, string>();
    private Dictionary<string, UserData> authIdToUserData = new Dictionary<string, UserData>();

    public NetworkServer(NetworkManager networkManager)
    {
        this.networkManager = networkManager;

        networkManager.ConnectionApprovalCallback += ApprovalCheck;
        networkManager.OnServerStarted += OnNetworkReady;
    }

    public bool OpenConnection(string ip, int port)
    {
        UnityTransport transport = networkManager.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, (ushort)port);
        return networkManager.StartServer();
    }

    private void ApprovalCheck(NetworkManager.ConnectionApprovalRequest request, NetworkManager.ConnectionApprovalResponse response)
    {
        string payload = System.Text.Encoding.UTF8.GetString(request.Payload);
        UserData userData = JsonUtility.FromJson<UserData>(payload);

        clientIdToAuth[request.ClientNetworkId] = userData.userName;
        authIdToUserData[userData.userAuthId] = userData;
        OnUserJoined?.Invoke(userData);

        (Vector3, Quaternion) posAndRot = SpawnPoint.GetRandomSpawnPos();
        response.Approved = true;
        response.Position = posAndRot.Item1;
        response.Rotation = posAndRot.Item2;
        response.CreatePlayerObject = true;
    }

    private void OnNetworkReady()
    {
        networkManager.OnClientDisconnectCallback += OnClientDisconnect;
    }

    public UserData GetUserData(ulong clientId)
    {
        if (clientIdToAuth.TryGetValue(clientId, out string authId))
        {
            if (authIdToUserData.TryGetValue(authId, out UserData userData))
            {
                return userData;
            }
        }
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
