using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using VContainer;

public class PlayerSpawnManager : IPlayerSpawnManager, IDisposable
{
    private INetworkServer networkServer;
    private IInputService inputService;
    private ISpawnPointService spawnPointService;

    public event Action<UserData, NetworkObject> OnPlayerSpawned;
    public event Action<UserData, NetworkObject> OnPlayerDespawned;

    [Inject]
    public void Construct(IInputService inputService, ISpawnPointService spawnPointService)
    {
        this.inputService = inputService;
        this.spawnPointService = spawnPointService;
        Debug.Log("[PlayerSpawnManager] Constructed, starting initialization with input service " + inputService);

        CoroutineMonoBehavior.Instance.StartCoroutine(Initialize());
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

        // Get a random unused spawn point first (we'll assign the network object after spawn)
        // Create instance first to get a reference for assignment
        var instance = UnityEngine.Object.Instantiate(server.PlayerPrefab);

        // Get a random unused spawn point and assign it to the network object
        var spawnData = spawnPointService.GetRandomUnusedSpawnPoint(instance);
        if (spawnData == null)
        {
            Debug.LogError("[PlayerSpawnManager] No spawn points available!");
            UnityEngine.Object.Destroy(instance);
            return;
        }

        // Set position and rotation before spawning
        instance.transform.position = spawnData.Position;
        instance.transform.rotation = spawnData.Rotation;

        // Now spawn the network object
        instance.SpawnAsPlayerObject(clientId);

        int playerIndex = instance.NetworkManager.ConnectedClients.Count - 1;

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

        Debug.Log($"[PlayerSpawnManager] Spawned player object for {userData.userName} at {spawnData.Position}");
    }

    private void HandleUserLeft(UserData userData)
    {
        Debug.Log($"[PlayerSpawnManager] Player {userData.userName} left.");

        // Release the spawn point if we can find the player's network object
        var server = ResolveNetworkServer();
        if (server != null && server.TryGetClientIdForUser(userData, out var clientId))
        {
            if (NetworkManager.Singleton != null &&
                NetworkManager.Singleton.SpawnManager != null &&
                NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId) != null)
            {
                var playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
                spawnPointService.ReleaseSpawnPoint(playerObject);
            }
        }
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

        // Count all existing players (including bots) to get unique index
        int totalPlayerCount = CountAllPlayersIncludingBots();

        // Instantiate bot player
        var botInstance = UnityEngine.Object.Instantiate(server.PlayerPrefab);

        // Add BotPlayerController component BEFORE spawning (so it's recognized as a bot)
        if (!botInstance.TryGetComponent<BotPlayerController>(out BotPlayerController botController))
        {
            botController = botInstance.gameObject.AddComponent<BotPlayerController>();
        }

        // Get a random unused spawn point and assign it to the bot network object
        // Do this BEFORE spawning to ensure the spawn point is marked as used
        var spawnData = spawnPointService.GetRandomUnusedSpawnPoint(botInstance);
        if (spawnData == null)
        {
            Debug.LogError("[PlayerSpawnManager] No spawn points available for bot!");
            UnityEngine.Object.Destroy(botInstance);
            return;
        }

        // Set position and rotation before spawning
        botInstance.transform.position = spawnData.Position;
        botInstance.transform.rotation = spawnData.Rotation;

        // Spawn as a network object (not as a player object, since bots don't have a client)
        botInstance.SpawnWithOwnership(NetworkManager.ServerClientId);

        // Initialize PlayerController for the bot AFTER spawning
        if (botInstance.TryGetComponent<PlayerController>(out PlayerController controller))
        {
            // Use the total player count as the bot's index to ensure unique colors
            // This includes all real players and any previously spawned bots
            controller.Initialize(totalPlayerCount);

            // Set bot name
            controller.PlayerName.Value = new Unity.Collections.FixedString32Bytes("Bot Player " + (totalPlayerCount + 1));
        }

        Debug.Log($"[PlayerSpawnManager] Spawned bot player at {spawnData.Position} with index {totalPlayerCount}");
    }

    /// <summary>
    /// Counts all players in the game, including both real players and bots.
    /// This is used to assign unique color indices.
    /// </summary>
    private int CountAllPlayersIncludingBots()
    {
        if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null)
            return 0;

        int count = 0;
        foreach (var spawnedObject in NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values)
        {
            if (spawnedObject != null && spawnedObject.TryGetComponent<PlayerController>(out _))
            {
                count++;
            }
        }

        return count;
    }

    public void Dispose()
    {
        networkServer.OnUserJoined -= HandleUserJoined;
        networkServer.OnUserLeft -= HandleUserLeft;
    }
}
