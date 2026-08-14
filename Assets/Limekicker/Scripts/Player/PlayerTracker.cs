using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

/// <summary>
/// Registry of spawned player network objects keyed by object id.
/// </summary>
public class PlayerTracker : IDisposable
{
    public static Dictionary<ulong, NetworkObject> players = new();

    private EventBinding<PlayerSpawnedEvent> playerSpawnEvent;

    public PlayerTracker()
    {
        playerSpawnEvent = new EventBinding<PlayerSpawnedEvent>(AddPlayer);
        EventBus<PlayerSpawnedEvent>.Register(playerSpawnEvent);
    }

    private void AddPlayer(PlayerSpawnedEvent playerSpawned)
    {
        if (playerSpawned.NetworkObject == null)
            return;

        ulong networkObjectId = playerSpawned.NetworkObject.NetworkObjectId;
        players[networkObjectId] = playerSpawned.NetworkObject;
    }

    public void RemovePlayer(ulong networkObjectId)
    {
        if (!players.TryGetValue(networkObjectId, out NetworkObject playerObject))
            return;

        ulong scoreClientId = networkObjectId;
        if (playerObject.TryGetComponent<PlayerController>(out PlayerController controller))
            scoreClientId = controller.IsBot ? controller.NetworkObjectId : controller.OwnerClientId;

        players.Remove(networkObjectId);

        EventBus<PlayerRemovedEvent>.Raise(new PlayerRemovedEvent
        {
            ClientId = scoreClientId,
            NetworkObjectId = networkObjectId
        });
    }

    /// <summary>
    /// Gets a player NetworkObject by their OwnerClientId (connection ID).
    /// Note: This works for real players. For bots, use GetPlayerByNetworkObjectId instead.
    /// </summary>
    public NetworkObject GetPlayerByID(ulong clientId)
    {
        return players.Values.FirstOrDefault(p => p.OwnerClientId == clientId);
    }

    /// <summary>
    /// Gets a player NetworkObject by their NetworkObjectId.
    /// Use this when you have a NetworkObjectId (e.g., from another NetworkObject).
    /// </summary>
    public NetworkObject GetPlayerByNetworkObjectId(ulong networkObjectId)
    {
        return players.TryGetValue(networkObjectId, out var player) ? player : null;
    }

    /// <summary>
    /// Gets a player by the ID used in scoring (OwnerClientId for real players, NetworkObjectId for bots).
    /// Use this when resolving the leading player from ScoreManager for victory cinematic.
    /// </summary>
    public NetworkObject GetPlayerByScoreClientId(ulong scoreClientId)
    {
        var byOwner = GetPlayerByID(scoreClientId);
        if (byOwner != null) return byOwner;
        return GetPlayerByNetworkObjectId(scoreClientId);
    }

    /// <summary>
    /// Gets a player NetworkObject that is NOT owned by the given clientId.
    /// Note: This works for real players. For bots, use GetPlayerByNetworkObjectId instead.
    /// </summary>
    public NetworkObject GetOtherPlayerByID(ulong clientId)
    {
        return players.Values.FirstOrDefault(p => p.OwnerClientId != clientId);
    }

    public IEnumerable<NetworkObject> GetAllPlayers()
    {
        return players.Values;
    }

    public void Dispose()
    {
        if (playerSpawnEvent != null)
        {
            EventBus<PlayerSpawnedEvent>.Unregister(playerSpawnEvent);
            playerSpawnEvent = null;
        }

        players.Clear();
    }
}
