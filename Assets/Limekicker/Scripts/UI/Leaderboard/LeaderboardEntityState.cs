using System;
using Unity.Collections;
using Unity.Netcode;

/// <summary>
/// Network-serializable leaderboard row data for a single player.
/// </summary>
public struct LeaderboardEntityState : INetworkSerializable, IEquatable<LeaderboardEntityState>
{
    public ulong ClientId;
    public FixedString32Bytes PlayerName;
    public int Cash;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref ClientId);
        serializer.SerializeValue(ref PlayerName);
        serializer.SerializeValue(ref Cash);
    }

    public bool Equals(LeaderboardEntityState other)
    {
        return
            ClientId == other.ClientId &&
            PlayerName.Equals(other.PlayerName) &&
            Cash == other.Cash;
    }
}
