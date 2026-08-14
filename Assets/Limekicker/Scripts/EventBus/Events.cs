using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Marker interface for payloads published through the typed event bus.
/// </summary>
public interface IEvent { }

/// <summary>
/// Empty marker payload for bus registrations that carry no data.
/// </summary>
public class Events : IEvent { }

/// <summary>
/// Announces when the active gameplay state changes.
/// </summary>
public struct GameStateChangeEvent : IEvent
{
    public IGameState NewState;
}

/// <summary>
/// Carries spawned player identity and network object references.
/// </summary>
public struct PlayerSpawnedEvent : IEvent
{
    public UserData UserData;
    public NetworkObject NetworkObject;
}

/// <summary>
/// Carries client and network object ids when a player leaves.
/// </summary>
public struct PlayerRemovedEvent : IEvent
{
    public ulong ClientId;
    public ulong NetworkObjectId;
}

/// <summary>
/// Carries countdown display text and numeric value updates.
/// </summary>
public struct CountdownEvent : IEvent
{
    public string CountdownValue;
    public int CountdownNumber;
}

/// <summary>
/// Reports who collected what, where, and with what magnitude.
/// </summary>
public struct CollectibleCollectedEvent : IEvent
{
    public ulong CollectorNetworkObjectId;
    public CollectibleType Type;
    public float Magnitude;
    public Vector3 WorldPosition;
}

/// <summary>
/// Signals that a player network object was teleported.
/// </summary>
public struct PlayerTeleportedEvent : IEvent
{
    public NetworkObject NetworkObject;
}

/// <summary>
/// Reports attacker client id and damage amount dealt.
/// </summary>
public struct DamageDealtEvent : IEvent
{
    public ulong AttackerClientId;
    public float DamageAmount;
}

/// <summary>
/// Severity levels for user-facing notification messages.
/// </summary>
public enum NotificationSeverity : byte
{
    Info = 0,
    Warning = 1,
    Error = 2
}

/// <summary>
/// Carries a user message and severity for UI display.
/// </summary>
public struct UserNotificationEvent : IEvent
{
    public string Message;
    public NotificationSeverity Severity;
}