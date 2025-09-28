using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class RespawnHandler : NetworkBehaviour
{
    [SerializeField] private NetworkObject playerPrefab;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        // Handle already spawned players (in case of host)
        PlayerController[] players = FindObjectsOfType<PlayerController>();
        foreach (var player in players)
        {
            HandlePlayerSpawned(player);
        }

        PlayerController.OnPlayerSpawned += HandlePlayerSpawned;
        PlayerController.OnPlayerDespawned += HandlePlayerDespawned;
    }

    private void HandlePlayerDespawned(PlayerController controller)
    {
        controller.DamageManager.OnCarDestroyed += () => HandlePlayerDie(controller);
    }

    private void HandlePlayerSpawned(PlayerController controller)
    {
        controller.DamageManager.OnCarDestroyed -= () => HandlePlayerDie(controller);
    }

    private void HandlePlayerDie(PlayerController controller)
    {
        Destroy(controller.gameObject);

        StartCoroutine(RespawnPlayer(controller.OwnerClientId));
    }

    private IEnumerator RespawnPlayer(ulong ownerClientId)
    {
        yield return null;

        (Vector3 spawnPosition, Quaternion spawnRotation) = SpawnPoint.GetRandomSpawnPos();
        NetworkObject playerInstance = Instantiate(playerPrefab, spawnPosition, spawnRotation);
        playerInstance.SpawnAsPlayerObject(ownerClientId);
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer)
            return;

        PlayerController.OnPlayerSpawned -= HandlePlayerSpawned;
        PlayerController.OnPlayerDespawned -= HandlePlayerDespawned;
    }
}
