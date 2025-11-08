using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using VContainer;

public class PlayerSpawnManager : IPlayerSpawnManager, IDisposable
{
    private INetworkServer networkServer;
    private IInputService inputService;

    public event Action<UserData, NetworkObject> OnPlayerSpawned;
    public event Action<UserData, NetworkObject> OnPlayerDespawned;

    [Inject]
    public void Construct(IInputService inputService)
    {
        this.inputService = inputService;
        Debug.Log("[PlayerSpawnManager] Constructed, starting initialization with input service " + inputService);
        CoroutineMonoBehavior.Instance.StartCoroutine(Initialize());
    }

    public IEnumerator Initialize()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton != null);

        yield return new WaitUntil(() => ResolveNetworkServer() != null);

        networkServer = ResolveNetworkServer();
        networkServer.OnUserJoined += HandleUserJoined;
        networkServer.OnUserLeft += HandleUserLeft;

        Debug.Log("[PlayerSpawnManager] Initialized and listening for joins.");

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
