using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using VContainer;

public class CollectibleSpawner : MonoBehaviour
{
    [SerializeField] private CollisionCollectible[] collectiblePrefabs;
    [SerializeField] private float spawnInterval;
    [SerializeField] private float spawnRadius = 60f;
    [SerializeField] private float minDistanceFromOthers;

    private float timer;
    private CollectibleType previouslySpawnedCollectible;
    private List<CollisionCollectible> activeCollectibles = new List<CollisionCollectible>();

    private IGameManager gameManager;

    [Inject]
    public void Construct(IGameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    private IEnumerator SpawnCollectibles()
    {
        while (true)
        {
            yield return new WaitForSeconds(spawnInterval);
            SpawnCollectible();
        }
    }

    private void SpawnCollectible()
    {
        Vector3 spawnPosition = GetPointOutOfReach();

        if (spawnPosition == Vector3.zero)
            return;

        // Raycast down to find the ground
        spawnPosition = Physics.Raycast(spawnPosition + Vector3.up * 50f, Vector3.down, out RaycastHit hitInfo, 100f) ? hitInfo.point : spawnPosition;
        spawnPosition += Vector3.up * 1.5f; // Slightly above ground

        CollisionCollectible prefabToSpawn;
        while (true)
        {
            prefabToSpawn = collectiblePrefabs[UnityEngine.Random.Range(0, collectiblePrefabs.Length)];

            if (previouslySpawnedCollectible == CollectibleType.None ||
                prefabToSpawn.CollectibleType != previouslySpawnedCollectible)
            {
                previouslySpawnedCollectible = prefabToSpawn.CollectibleType;
                break;
            }
        }

        NetworkObject spawnedCollectible = Instantiate(prefabToSpawn.NetworkObject, spawnPosition, Quaternion.identity);
        activeCollectibles.Add(spawnedCollectible.GetComponent<CollisionCollectible>());
    }

    private Vector3 GetPointOutOfReach()
    {
        List<Vector3> existingPositions = new List<Vector3>();

        foreach (var player in gameManager.PlayerTracker.GetAllPlayers())
        {
            existingPositions.Add(player.transform.position);
        }
        foreach (var collectible in activeCollectibles)
        {
            existingPositions.Add(collectible.transform.position);
        }

        Vector3 randomPoint;
        int attempts = 0;

        while (true)
        {
            randomPoint = UnityEngine.Random.insideUnitSphere * spawnRadius;
            randomPoint.y = 0; // Keep on ground level
            attempts++;
            if (attempts > 100)
            {
                Debug.LogWarning("Could not find a suitable spawn point after 100 attempts.");
                return Vector3.zero;
            }

            if (!IsPointTooClose(randomPoint, existingPositions))
                return randomPoint;
        }
    }

    private bool IsPointTooClose(Vector3 randomPoint, List<Vector3> existingPositions)
    {
        for (int i = 0; i < existingPositions.Count; i++)
        {
            if (Vector3.Distance(randomPoint, existingPositions[i]) < minDistanceFromOthers)
                return true;
        }

        return false;
    }
}
