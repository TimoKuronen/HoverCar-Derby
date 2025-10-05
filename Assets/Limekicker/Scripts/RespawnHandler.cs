using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class RespawnHandler : NetworkBehaviour
{
    [SerializeField] private PlayerController playerPrefab;
    [SerializeField] private float cashKeptPercentage = 0.8f;

    private int currentCash;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
            return;

        // Handle already spawned players (in case of host)
        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);
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
        currentCash = (int)(controller.Cash * (cashKeptPercentage / 100));

        Destroy(controller.gameObject);

        StartCoroutine(RespawnPlayer(controller.OwnerClientId));
    }

    private IEnumerator RespawnPlayer(ulong ownerClientId)
    {
        yield return null;

        (Vector3 spawnPosition, Quaternion spawnRotation) = SpawnPoint.GetRandomSpawnPos();
        
        PlayerController playerInstance = Instantiate(playerPrefab, spawnPosition, spawnRotation);

        playerInstance.NetworkObject.SpawnAsPlayerObject(ownerClientId);

        // set cash to the saved value
    }

    public override void OnNetworkDespawn()
    {
        if (!IsServer)
            return;

        PlayerController.OnPlayerSpawned -= HandlePlayerSpawned;
        PlayerController.OnPlayerDespawned -= HandlePlayerDespawned;
    }
}
