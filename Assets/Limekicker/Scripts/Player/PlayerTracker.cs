using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

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
        // Store by NetworkObjectId to handle bots correctly (bots share OwnerClientId with server)
        // But provide lookup methods that search by OwnerClientId for real players
        players.Add(playerSpawned.NetworkObject.NetworkObjectId, playerSpawned.NetworkObject);
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
