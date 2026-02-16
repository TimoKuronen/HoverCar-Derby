using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class SpawnPointService
{
    private SpawnPoint[] allSpawnPoints;
    private Dictionary<NetworkObject, SpawnPointData> assignedSpawnPoints = new();
    private HashSet<SpawnPoint> usedSpawnPoints = new();
    public SpawnPointService() { }

    public void Initialize()
    {
        allSpawnPoints = Object.FindObjectsOfType<SpawnPoint>(true)
            .OrderBy(sp => sp.transform.position.x)
            .ToArray();

        assignedSpawnPoints.Clear();
        usedSpawnPoints.Clear();
    }

    /// <summary>
    /// Gets a random unused spawn point for a network object. If all spawn points are used,
    /// resets the used set to allow reuse. Marks spawn point as used immediately to prevent race conditions.
    /// </summary>
    public SpawnPointData GetRandomUnusedSpawnPoint(NetworkObject networkObject)
    {
        if (allSpawnPoints == null || allSpawnPoints.Length == 0)
        {
            Debug.LogError("[SpawnPointService] No spawn points available!");
            return null;
        }

        // Check if this network object already has a spawn point assigned
        if (networkObject != null && assignedSpawnPoints.ContainsKey(networkObject))
        {
            Debug.LogWarning($"[SpawnPointService] NetworkObject {networkObject.name} already has a spawn point assigned!");
            return assignedSpawnPoints[networkObject];
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

        var selectedSpawnPoint = unusedSpawnPoints[Random.Range(0, unusedSpawnPoints.Length)];

        usedSpawnPoints.Add(selectedSpawnPoint);

        var spawnData = new SpawnPointData
        {
            SpawnPoint = selectedSpawnPoint,
            AssignedObject = networkObject
        };

        // Store the assignment IMMEDIATELY
        if (networkObject != null)
        {
            assignedSpawnPoints[networkObject] = spawnData;
        }

        //Debug.Log($"[SpawnPointService] Assigned spawn point at {selectedSpawnPoint.transform.position} to {networkObject?.name ?? "null"} ({usedSpawnPoints.Count}/{allSpawnPoints.Length} used)");

        return spawnData;
    }

    /// <summary>
    /// Gets the spawn point furthest from the given network object. Returns existing assignment if available.
    /// </summary>
    public SpawnPointData GetFurthestSpawnpoint(NetworkObject networkObject)
    {
        int furthestWaypointIndex = -1;
        float furthestDistance = 0f;
        for (int i = 0; i < allSpawnPoints.Length; i++)
        {
            float distance = Vector3.Distance(allSpawnPoints[i].transform.position, networkObject.transform.position);
            if (distance > furthestDistance)
            {
                furthestDistance = distance;
                furthestWaypointIndex = i;
            }
        }

        return assignedSpawnPoints.ContainsKey(networkObject) ? assignedSpawnPoints[networkObject] : new SpawnPointData
        {
            SpawnPoint = allSpawnPoints[furthestWaypointIndex],
            AssignedObject = networkObject
        };
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

/// <summary>
/// Data structure containing spawn point information.
/// </summary>
public class SpawnPointData
{
    public SpawnPoint SpawnPoint { get; set; }
    public Vector3 Position => SpawnPoint.transform.position;
    public Quaternion Rotation => SpawnPoint.transform.rotation;
    public NetworkObject AssignedObject { get; set; }
}