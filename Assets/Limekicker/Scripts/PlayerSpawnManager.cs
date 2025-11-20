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
            
            // Spawn bot for testing if enabled
            if (MainMenu.IsSpawnBotEnabled())
            {
                yield return new WaitForSeconds(0.5f); // Small delay to ensure everything is initialized
                SpawnBotPlayer();
            }

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
        
        // Clamp playerIndex to valid spawn point range
        int clampedIndex = Mathf.Clamp(playerIndex, 0, spawnPoints.Length - 1);
        instance.transform.SetPositionAndRotation(spawnPoints[clampedIndex].transform.position, spawnPoints[clampedIndex].transform.rotation);
        Debug.Log($"[PlayerSpawnManager] setting player position based on index {playerIndex} (clamped to {clampedIndex})");
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

    /// <summary>Spawns a bot player for testing collisions.</summary>
    private void SpawnBotPlayer()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;
        
        var server = ResolveNetworkServer();
        if (server == null || server.PlayerPrefab == null)
        {
            Debug.LogWarning("[PlayerSpawnManager] Cannot spawn bot: NetworkServer or PlayerPrefab is null.");
            return;
        }
        
        // Find an available spawn point - use the last spawn point to avoid conflicts
        int spawnIndex = spawnPoints.Length > 0 ? spawnPoints.Length - 1 : 0;
        Vector3 spawnPos = spawnPoints[spawnIndex].transform.position;
        Quaternion spawnRot = spawnPoints[spawnIndex].transform.rotation;
        
        // Instantiate bot player
        var botInstance = UnityEngine.Object.Instantiate(server.PlayerPrefab, spawnPos, spawnRot);
        
        // Add BotPlayerController component BEFORE spawning (so it's recognized as a bot)
        if (!botInstance.TryGetComponent<BotPlayerController>(out BotPlayerController botController))
        {
            botController = botInstance.gameObject.AddComponent<BotPlayerController>();
        }
        
        // Spawn as a network object (not as a player object, since bots don't have a client)
        botInstance.SpawnWithOwnership(NetworkManager.ServerClientId);
        
        // Initialize PlayerController for the bot AFTER spawning
        if (botInstance.TryGetComponent<PlayerController>(out PlayerController controller))
        {
            // Use index 0 for bot (will be overridden by color assignment, but safe)
            // The bot won't trigger camera changes because it's not owned by a client
            int botIndex = 0; // Use 0, but bot won't interfere since it's not a real player
            controller.Initialize(botIndex);
            
            // Set bot name
            controller.PlayerName.Value = new Unity.Collections.FixedString32Bytes("Bot Player");
            
            // IMPORTANT: Prevent bot from triggering camera/control events
            // The bot's PlayerController should not fire OnPlayerSpawned events that affect camera
        }
        
        // Set up CarColorPainter for bot - use a valid color index (0-7)
        if (botInstance.TryGetComponent<CarColorPainter>(out CarColorPainter colorPainter))
        {
            // Use a valid color index (assuming you have at least 1 color)
            colorPainter.AssignColor(0);
        }
        
        Debug.Log($"[PlayerSpawnManager] Spawned bot player at {spawnPos} (spawn index: {spawnIndex})");
    }
    
    public void Dispose()
    {
        networkServer.OnUserJoined -= HandleUserJoined;
        networkServer.OnUserLeft -= HandleUserLeft;
    }
}
