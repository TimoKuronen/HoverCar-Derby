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