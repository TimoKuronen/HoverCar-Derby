using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class PlayerTracker
{
    private IPlayerSpawnManager playerSpawnManager;
    public static Dictionary<ulong, NetworkObject> players = new();

    public PlayerTracker(IPlayerSpawnManager playerSpawnManager)
    {
        this.playerSpawnManager = playerSpawnManager;
        playerSpawnManager.OnPlayerSpawned += AddPlayer;
    }

    private void AddPlayer(UserData data, NetworkObject playerObject)
    {
        players.Add(playerObject.NetworkObjectId, playerObject);
    }

    public NetworkObject GetPlayerByID(ulong clientId)
    {
        return players.Values.FirstOrDefault(p => p.OwnerClientId == clientId);
    }
}
