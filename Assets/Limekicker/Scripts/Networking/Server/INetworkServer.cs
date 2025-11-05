using System;
using System.Collections.Generic;

public interface INetworkServer
{
    event Action<UserData> OnUserJoined;
    event Action<UserData> OnUserLeft;

    IReadOnlyList<UserData> GetConnectedUsers();
}