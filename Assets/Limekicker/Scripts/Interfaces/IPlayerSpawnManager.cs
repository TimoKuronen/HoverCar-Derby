using System;
using Unity.Netcode;

public interface IPlayerSpawnManager
{
    event Action<UserData, NetworkObject> OnPlayerSpawned;
    event Action<UserData, NetworkObject> OnPlayerDespawned;
}