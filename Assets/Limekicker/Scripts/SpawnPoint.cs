using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPoint : MonoBehaviour
{
    private static List<SpawnPoint> spawnPoints = new List<SpawnPoint>();

    private void OnEnable()
    {
        spawnPoints.Add(this);
    }

    public static (Vector3, Quaternion) GetRandomSpawnPos()
    {
        if (spawnPoints.Count == 0)
        {
            Debug.LogWarning("No spawn points available!");
            return (Vector3.zero, Quaternion.identity);
        }
        int randomIndex = Random.Range(0, spawnPoints.Count);
        return (spawnPoints[randomIndex].transform.position, spawnPoints[randomIndex].transform.rotation);
    }

    private void OnDisable()
    {
        spawnPoints.Remove(this);
    }

#if UNITY_EDITOR
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawSphere(transform.position, 1f);
    }
#endif
}