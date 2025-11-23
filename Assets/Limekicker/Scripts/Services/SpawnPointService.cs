using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class SpawnPointService : ISpawnPointService
{
    private SpawnPoint[] allSpawnPoints;
    private Dictionary<NetworkObject, SpawnPointData> assignedSpawnPoints = new Dictionary<NetworkObject, SpawnPointData>();
    private HashSet<SpawnPoint> usedSpawnPoints = new HashSet<SpawnPoint>();

    public SpawnPointService()
    {
        Initialize();
    }

    private void Initialize()
    {
        allSpawnPoints = Object.FindObjectsOfType<SpawnPoint>()
            .OrderBy(sp => sp.transform.position.x)
            .ToArray();

        Debug.Log($"[SpawnPointService] Initialized with {allSpawnPoints.Length} spawn points");
    }

    public SpawnPointData GetRandomUnusedSpawnPoint(NetworkObject networkObject)
    {
        if (allSpawnPoints == null || allSpawnPoints.Length == 0)
        {
            Debug.LogWarning("[SpawnPointService] No spawn points available!");
            return null;
        }

        // Get all unused spawn points
        var unusedSpawnPoints = allSpawnPoints
            .Where(sp => !usedSpawnPoints.Contains(sp))
            .ToArray();

        if (unusedSpawnPoints.Length == 0)
        {
            Debug.LogWarning("[SpawnPointService] All spawn points are in use! Reusing spawn points.");
            // If all are used, reset and reuse (for respawning scenarios)
            usedSpawnPoints.Clear();
            unusedSpawnPoints = allSpawnPoints;
        }

        // Pick a random unused spawn point
        var selectedSpawnPoint = unusedSpawnPoints[Random.Range(0, unusedSpawnPoints.Length)];
        
        // Mark as used
        usedSpawnPoints.Add(selectedSpawnPoint);

        var spawnData = new SpawnPointData
        {
            SpawnPoint = selectedSpawnPoint,
            AssignedObject = networkObject
        };

        // Store the assignment
        if (networkObject != null)
        {
            assignedSpawnPoints[networkObject] = spawnData;
        }

        Debug.Log($"[SpawnPointService] Assigned spawn point at {selectedSpawnPoint.transform.position} to {networkObject?.name ?? "null"} ({usedSpawnPoints.Count}/{allSpawnPoints.Length} used)");
        
        return spawnData;
    }

    public SpawnPointData GetSpawnPointForObject(NetworkObject networkObject)
    {
        if (networkObject == null)
            return null;

        if (assignedSpawnPoints.TryGetValue(networkObject, out var spawnData))
        {
            return spawnData;
        }

        return null;
    }

    public void ReleaseSpawnPoint(NetworkObject networkObject)
    {
        if (networkObject == null)
            return;

        if (assignedSpawnPoints.TryGetValue(networkObject, out var spawnData))
        {
            usedSpawnPoints.Remove(spawnData.SpawnPoint);
            assignedSpawnPoints.Remove(networkObject);
            Debug.Log($"[SpawnPointService] Released spawn point for {networkObject.name}");
        }
    }

    public SpawnPoint[] GetAllSpawnPoints()
    {
        return allSpawnPoints;
    }

}

