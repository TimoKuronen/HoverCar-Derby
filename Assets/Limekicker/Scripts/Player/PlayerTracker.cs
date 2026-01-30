using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;

public class PlayerTracker
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
        players.Add(playerSpawned.NetworkObject.NetworkObjectId, playerSpawned.NetworkObject);
    }

    public NetworkObject GetPlayerByID(ulong clientId)
    {
        return players.Values.FirstOrDefault(p => p.OwnerClientId == clientId);
    }

    public NetworkObject GetOtherPlayerByID(ulong clientId)
    {
        return players.Values.FirstOrDefault(p => p.OwnerClientId != clientId);
    }
}
