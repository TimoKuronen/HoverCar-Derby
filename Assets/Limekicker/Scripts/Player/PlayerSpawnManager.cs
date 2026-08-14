using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-side spawner for players and bots at available spawn points.
/// </summary>
public class PlayerSpawnManager : IDisposable
{
    private INetworkServer networkServer;
    private readonly IInputService inputService;
    private readonly IGameManager gameManager;

    private SpawnPointService spawnPointService = new();
    private HashSet<ulong> spawnedClientIds = new();
    private bool botSpawned = false;

    public PlayerSpawnManager(IInputService inputService, IGameManager gameManager)
    {
        this.gameManager = gameManager;
        this.inputService = inputService;
    }

    public IEnumerator Initialize()
    {
        CarManager.OnCarRespawned += HandleCarRespawn;
        spawnPointService.Initialize();

        yield return new WaitUntil(() => NetworkManager.Singleton != null);

        if (NetworkManager.Singleton.IsServer || NetworkManager.Singleton.IsHost)
        {
            yield return new WaitUntil(() => ResolveNetworkServer() != null);

            networkServer = ResolveNetworkServer();
            
            foreach (var existing in networkServer.GetConnectedUsers())
            {
                HandleUserJoined(existing);
            }
            
            networkServer.OnUserJoined += HandleUserJoined;
            networkServer.OnUserLeft += HandleUserLeft;

            yield return new WaitForSeconds(1);

            if (DevMenuOptions.IsSpawnBotEnabled())
            {
                yield return new WaitForSeconds(0.5f);
                SpawnBotPlayer();
            }
        }
        else
        {
            Debug.Log("[PlayerSpawnManager] Client mode - waiting for local player to spawn...");

            yield return new WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient);

            NetworkObject localPlayer = null;
            yield return new WaitUntil(() =>
            {
                if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
                    return false;

                localPlayer = NetworkManager.Singleton.SpawnManager?.GetLocalPlayerObject();
                return localPlayer != null;
            });

            Debug.Log("[PlayerSpawnManager] Local player spawned on client - waiting for PlayerController spawn event.");
        }
    }
    /// <summary>
    /// Handles car respawn by finding a spawn point away from other players and teleporting.
    /// </summary>
    private void HandleCarRespawn(CarManager obj, System.Action onTeleportComplete)
    {
        NetworkObject otherPlayer = null;
        if (obj.PlayerController.IsBot)
        {
            var allPlayers = gameManager.PlayerTracker.GetAllPlayers();
            otherPlayer = allPlayers.FirstOrDefault(p => p != obj.PlayerController.NetworkObject);
        }
        else
        {
            otherPlayer = gameManager.PlayerTracker.GetOtherPlayerByID(obj.PlayerController.OwnerClientId);
        }

        var spawnData = spawnPointService.GetRandomUnusedSpawnPoint(otherPlayer);

        ulong clientId = obj.PlayerController.IsBot ? obj.PlayerController.NetworkObjectId : obj.PlayerController.OwnerClientId;
        CoroutineMonoBehavior.Instance.StartCoroutine(TeleportAfterSpawn(
            obj.PlayerController.NetworkObject,
            spawnData,
            clientId,
            onTeleportComplete));
    }

    private INetworkServer ResolveNetworkServer()
    {
        return NetworkSession.GetNetworkServer();
    }

    /// <summary>
    /// Spawns a player when they join. Prevents duplicate spawns and handles spawn point assignment.
    /// </summary>
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

        if (spawnedClientIds.Contains(clientId))
        {
            Debug.Log($"[PlayerSpawnManager] Player for {userData.userName} (Client ID: {clientId}) already tracked as spawned, skipping duplicate spawn.");
            return;
        }

        if (NetworkManager.Singleton.SpawnManager != null && 
            NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId) != null)
        {
            Debug.Log($"[PlayerSpawnManager] Player for {userData.userName} (Client ID: {clientId}) already exists, skipping spawn.");
            spawnedClientIds.Add(clientId);
            return;
        }

        spawnedClientIds.Add(clientId);

        var instance = UnityEngine.Object.Instantiate(server.PlayerPrefab);
        instance.gameObject.SetActive(false);

        var spawnData = spawnPointService.GetRandomUnusedSpawnPoint(instance);
        if (spawnData == null)
        {
            Debug.LogError("[PlayerSpawnManager] No spawn points available!");
            UnityEngine.Object.Destroy(instance);
            return;
        }

        instance.transform.SetPositionAndRotation(spawnData.Position, spawnData.Rotation);
        instance.gameObject.SetActive(true);
        instance.SpawnAsPlayerObject(clientId);

        CoroutineMonoBehavior.Instance.StartCoroutine(TeleportAfterSpawn(instance, spawnData, clientId));

        if (instance.TryGetComponent<PlayerController>(out PlayerController controller))
        {
            int joinOrderOneBased = instance.NetworkManager.ConnectedClients.Count;
            controller.Initialize(joinOrderOneBased - 1);
            ApplyDisplayName(controller, userData.userName, joinOrderOneBased);
        }
        else
        {
            Debug.LogWarning($"[PlayerSpawnManager] Could not find PlayerController on spawned instance for {userData.userName}");
        }

        gameManager.RegisterParticipant(instance.NetworkObjectId);
    }

    private void HandleUserLeft(UserData userData)
    {
        var server = ResolveNetworkServer();
        if (server == null || !server.TryGetClientIdForUser(userData, out var clientId))
            return;

        spawnedClientIds.Remove(clientId);

        if (NetworkManager.Singleton == null || NetworkManager.Singleton.SpawnManager == null)
            return;

        var playerObject = NetworkManager.Singleton.SpawnManager.GetPlayerNetworkObject(clientId);
        if (playerObject == null)
            return;

        spawnPointService.ReleaseSpawnPoint(playerObject);
        gameManager.UnregisterParticipant(playerObject.NetworkObjectId);
        gameManager.PlayerTracker?.RemovePlayer(playerObject.NetworkObjectId);
        playerObject.Despawn(true);
    }

    private static void ApplyDisplayName(PlayerController controller, string userName, int joinOrderOneBased)
    {
        if (PlayerDisplayNameUtility.ShouldUseJoinOrderName(userName))
            controller.PlayerName.Value = PlayerDisplayNameUtility.BuildJoinOrderName(joinOrderOneBased);
    }

    /// <summary>
    /// Spawns a bot player for testing. Bots use NetworkObjectId instead of OwnerClientId.
    /// </summary>
    private void SpawnBotPlayer()
    {
        if (!NetworkManager.Singleton.IsServer)
            return;

        if (botSpawned)
        {
            Debug.LogWarning("[PlayerSpawnManager] Bot already spawned, skipping duplicate call.");
            return;
        }

        botSpawned = true;

        var server = ResolveNetworkServer();
        if (server == null || server.PlayerPrefab == null)
        {
            Debug.LogWarning("[PlayerSpawnManager] Cannot spawn bot: NetworkServer or PlayerPrefab is null.");
            botSpawned = false;
            return;
        }

        int totalPlayerCount = CountAllPlayersIncludingBots();

        var botInstance = UnityEngine.Object.Instantiate(server.PlayerPrefab);

        if (!botInstance.TryGetComponent<BotPlayerController>(out BotPlayerController botController))
        {
            botController = botInstance.gameObject.AddComponent<BotPlayerController>();
        }

        var spawnData = spawnPointService.GetRandomUnusedSpawnPoint(botInstance);
        if (spawnData == null)
        {
            Debug.LogError("[PlayerSpawnManager] No spawn points available for bot!");
            UnityEngine.Object.Destroy(botInstance);
            return;
        }

        botInstance.transform.SetPositionAndRotation(spawnData.Position, spawnData.Rotation);
        botInstance.SpawnWithOwnership(NetworkManager.ServerClientId);

        CoroutineMonoBehavior.Instance.StartCoroutine(TeleportAfterSpawn(botInstance, spawnData, NetworkManager.ServerClientId));

        if (botInstance.TryGetComponent<PlayerController>(out PlayerController controller))
        {
            controller.Initialize(totalPlayerCount);
            controller.PlayerName.Value = new Unity.Collections.FixedString32Bytes("Bot Player " + (totalPlayerCount + 1));
        }

        gameManager.RegisterParticipant(botInstance.NetworkObjectId);
        gameManager.MarkParticipantReady(botInstance.NetworkObjectId);
    }

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

    /// <summary>
    /// Teleports a NetworkObject to spawn position after NetworkTransform initializes.
    /// Player cars use ClientNetworkTransform, so only the owning client may call Teleport.
    /// Bots and other server-owned objects are teleported on the server directly.
    /// </summary>
    private IEnumerator TeleportAfterSpawn(NetworkObject networkObject, SpawnPointData spawnData, ulong clientId, System.Action onTeleportComplete = null)
    {
        yield return null;

        if (networkObject == null || spawnData == null)
        {
            onTeleportComplete?.Invoke();
            yield break;
        }

        bool usesClientAuthority = networkObject.TryGetComponent<ClientNetworkTransform>(out _);
        networkObject.TryGetComponent<PlayerController>(out var controller);
        bool isPlayerCar = controller != null && !controller.IsBot;

        if (usesClientAuthority && isPlayerCar)
        {
            yield return new WaitForSeconds(0.1f);
            controller.TeleportToSpawnPositionClientRpc(spawnData.Position, spawnData.Rotation);
        }
        else if (networkObject.TryGetComponent<Unity.Netcode.Components.NetworkTransform>(out var networkTransform))
        {
            networkTransform.Teleport(spawnData.Position, spawnData.Rotation, Vector3.one);
        }
        else
        {
            networkObject.transform.SetPositionAndRotation(spawnData.Position, spawnData.Rotation);
            Debug.LogWarning($"[PlayerSpawnManager] NetworkTransform not found on {networkObject.name}, using direct transform set");
        }

        onTeleportComplete?.Invoke();
    }

    public void Dispose()
    {
        if (networkServer != null)
        {
            networkServer.OnUserJoined -= HandleUserJoined;
            networkServer.OnUserLeft -= HandleUserLeft;
        }

        CarManager.OnCarRespawned -= HandleCarRespawn;
    }
}
