using Unity.Netcode;
using UnityEngine;

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
    public ulong CollectorNetworkObjectId;
    public CollectibleType Type;
    public float Magnitude;
    public Vector3 WorldPosition;
}

public struct PlayerTeleportedEvent : IEvent
{
    public NetworkObject NetworkObject;
}

public struct DamageDealtEvent : IEvent
{
    public ulong AttackerClientId;
    public float DamageAmount;
}

public enum NotificationSeverity : byte
{
    Info = 0,
    Warning = 1,
    Error = 2
}

public struct UserNotificationEvent : IEvent
{
    public string Message;
    public NotificationSeverity Severity;
}