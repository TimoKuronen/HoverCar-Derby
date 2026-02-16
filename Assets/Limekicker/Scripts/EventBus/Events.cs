using Unity.Netcode;

public interface IEvent { }

public class Events : IEvent { }

public struct GameStateChangeEvent : IEvent
{
    public IGameState NewState;
}

public struct PlayerSpawnedEvent : IEvent
{
    public UserData UserData;
    public NetworkObject NetworkObject;
}

public struct CountdownEvent : IEvent
{
    public string CountdownValue;
    public int CountdownNumber;
}

public struct CollectibleCollectedEvent : IEvent
{
    public ulong PlayerNetworkObjectId;
    public CollectibleType CollectibleType;
}

public struct PlayerTeleportedEvent : IEvent
{
    public NetworkObject NetworkObject;
}