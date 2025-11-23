using Unity.Netcode;
using UnityEngine;

public interface ISpawnPointService
{
    /// <summary>
    /// Gets a random unused spawn point, marks it as used, and assigns it to the network object.
    /// Returns null if no spawn points are available.
    /// </summary>
    SpawnPointData GetRandomUnusedSpawnPoint(NetworkObject networkObject);

    /// <summary>
    /// Gets the spawn point data assigned to a specific network object.
    /// Returns null if not found.
    /// </summary>
    SpawnPointData GetSpawnPointForObject(NetworkObject networkObject);

    /// <summary>
    /// Releases a spawn point, making it available for use again.
    /// </summary>
    void ReleaseSpawnPoint(NetworkObject networkObject);

    /// <summary>
    /// Gets all spawn points (for debugging or other purposes).
    /// </summary>
    SpawnPoint[] GetAllSpawnPoints();
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

