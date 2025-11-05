using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using VContainer;

public class PlayerSpawnManager : IPlayerSpawnManager, IDisposable
{
    private INetworkServer networkServer;

    [Inject]
    public void Construct()
    {
        Debug.Log("[PlayerSpawnManager] Constructed, starting initialization...");
        CoroutineMonoBehavior.Instance.StartCoroutine(Initialize());
    }

    public IEnumerator Initialize()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton != null);

        yield return new WaitUntil(() => ResolveNetworkServer() != null);

        Debug.Log("[PlayerSpawnManager] Initialized and listening for joins.");

        networkServer = ResolveNetworkServer();

        foreach (var existing in networkServer.GetConnectedUsers())
        {
            HandleUserJoined(existing);
        }
    }

    private INetworkServer ResolveNetworkServer()
    {
        if (NetworkManager.Singleton == null) return null;

        // Host path
        if (NetworkManager.Singleton.IsHost &&
            HostSingleton.Instance != null &&
            HostSingleton.Instance.GameManager != null &&
            HostSingleton.Instance.GameManager.NetworkServer != null)
        {
            return HostSingleton.Instance.GameManager.NetworkServer;
        }

        // Dedicated server path
        if (NetworkManager.Singleton.IsServer &&
            ServerSingleton.Instance != null &&
            ServerSingleton.Instance.GameManager != null &&
            ServerSingleton.Instance.GameManager.NetworkServer != null)
        {
            return ServerSingleton.Instance.GameManager.NetworkServer;
        }

        return null;
    }

    private void HandleUserJoined(UserData userData)
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        Vector3 spawnPos = SpawnPoint.GetRandomSpawnPos().Item1;
        Quaternion spawnRot = Quaternion.identity;

        NetworkObject playerPrefab = HostSingleton.Instance.GameManager.NetworkServer.PlayerPrefab;
        var instance = UnityEngine.Object.Instantiate(playerPrefab, spawnPos, spawnRot);
        instance.SpawnAsPlayerObject(GetClientIdForUser(userData));

        Debug.Log($"[PlayerSpawnManager] Spawned player object for {userData.userName} at {spawnPos}");
    }

    private ulong GetClientIdForUser(UserData userData)
    {
        // Your existing mapping logic lives in NetworkServer;
        // temporarily expose a method for lookup.
        if (NetworkManager.Singleton.ConnectedClientsIds.Count > 0)
        {
            foreach (var clientId in NetworkManager.Singleton.ConnectedClientsIds)
            {
                if (NetworkManager.Singleton.ConnectedClients[clientId]
                    .PlayerObject.OwnerClientId == clientId)
                {
                    return clientId;
                }
            }
        }
        Debug.LogWarning($"[PlayerSpawnManager] Couldn't find client ID for {userData.userName}");
        return 0;
    }

    private void HandleUserLeft(UserData userData)
    {
        Debug.Log($"[PlayerSpawnManager] Player {userData.userName} left.");
        // Optional: cleanup or respawn logic
    }

    public void Dispose()
    {
        networkServer.OnUserJoined -= HandleUserJoined;
        networkServer.OnUserLeft -= HandleUserLeft;
    }
}
