using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using VContainer;

public class PlayerSpawnManager : IPlayerSpawnManager, IDisposable
{
    private INetworkServer networkServer;
    private IInputService inputService;

    public event Action<UserData, NetworkObject> OnPlayerSpawned;
    public event Action<UserData, NetworkObject> OnPlayerDespawned;

    private SpawnPoint[] spawnPoints;

    [Inject]
    public void Construct(IInputService inputService)
    {
        this.inputService = inputService;
        Debug.Log("[PlayerSpawnManager] Constructed, starting initialization with input service " + inputService);
        spawnPoints = GameObject.FindObjectsOfType<SpawnPoint>();

        ShuffleWaypoints();

        CoroutineMonoBehavior.Instance.StartCoroutine(Initialize());
    }

    private void ShuffleWaypoints()
    {
        Transform[] points = new Transform[spawnPoints.Length];
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            points[i] = spawnPoints[i].transform;
        }

        System.Random rng = new System.Random();
        int n = points.Length;
        while (n > 1)
        {
            int k = rng.Next(n--);
            Transform temp = points[n];
            points[n] = points[k];
            points[k] = temp;
        }
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            spawnPoints[i].transform.SetPositionAndRotation(points[i].position, points[i].rotation);
        }
    }

    public IEnumerator Initialize()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton != null);

        // Server/host path: Initialize server-side spawn management
        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
        {
            yield return new WaitUntil(() => ResolveNetworkServer() != null);

            networkServer = ResolveNetworkServer();
            networkServer.OnUserJoined += HandleUserJoined;
            networkServer.OnUserLeft += HandleUserLeft;

            Debug.Log("[PlayerSpawnManager] Initialized and listening for joins.");

            foreach (var existing in networkServer.GetConnectedUsers())
            {
                HandleUserJoined(existing);
            }

            yield return new WaitForSeconds(1);

            GameSignals.MarkSessionLoaded();
        }
        else
        {
            // Client path: Wait for local player object to spawn, then mark session loaded
            Debug.Log("[PlayerSpawnManager] Client mode - waiting for local player to spawn...");

            yield return new WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient);

            // Wait for local player object to be spawned
            NetworkObject localPlayer = null;
            yield return new WaitUntil(() => 
            {
                if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
                    return false;

                localPlayer = NetworkManager.Singleton.SpawnManager?.GetLocalPlayerObject();
                return localPlayer != null;
            });

            Debug.Log("[PlayerSpawnManager] Local player spawned on client - firing OnPlayerSpawned event and marking session loaded.");
            
            // Fire OnPlayerSpawned event for clients so SimpleHoverChaseCam can attach
            // UserData is null on clients, but netObj is what we need
            if (localPlayer != null)
            {
                OnPlayerSpawned?.Invoke(null, localPlayer);
            }
            
            GameSignals.MarkSessionLoaded();
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

        var server = ResolveNetworkServer();
        if (server == null || server.PlayerPrefab == null)
        {
            Debug.LogWarning("[PlayerSpawnManager] NetworkServer or PlayerPrefab is null; aborting spawn.");
            return;
        }

        if (!server.TryGetClientIdForUser(userData, out var clientId))
        {
            Debug.LogWarning($"[PlayerSpawnManager] Could not resolve clientId for {userData.userName}; aborting spawn.");
            return;
        }

        var instance = UnityEngine.Object.Instantiate(server.PlayerPrefab, spawnPos, spawnRot);
        instance.SpawnAsPlayerObject(clientId);
        int playerIndex = instance.NetworkManager.ConnectedClients.Count - 1;
        instance.transform.SetPositionAndRotation(spawnPoints[playerIndex].transform.position, spawnPoints[playerIndex].transform.rotation);

        if (instance.TryGetComponent<HoverCarMover>(out HoverCarMover mover) && inputService != null)
        {
            mover.Construct(inputService);
        }
        else
        {
            Debug.LogWarning($"[PlayerSpawnManager] mover: {mover}, inputService: {inputService}");
        }
        if (instance.TryGetComponent<PlayerController>(out PlayerController controller))
        {
            controller.Initialize(instance.NetworkManager.ConnectedClients.Count - 1);
        }
        else
        {
            Debug.LogWarning($"[PlayerSpawnManager] Could not find PlayerController on spawned instance for {userData.userName}");
        }

        OnPlayerSpawned?.Invoke(userData, instance);

        Debug.Log($"[PlayerSpawnManager] Spawned player object for {userData.userName} at {spawnPos}");
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
