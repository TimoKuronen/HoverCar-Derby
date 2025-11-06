using System;
using System.Collections.Generic;
using Unity.Netcode;

public interface INetworkServer
{
    event Action<UserData> OnUserJoined;
    event Action<UserData> OnUserLeft;

    NetworkObject PlayerPrefab { get; }
    bool TryGetClientIdForUser(UserData userData, out ulong clientId);

    IReadOnlyList<UserData> GetConnectedUsers();
}