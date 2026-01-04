using System.Collections;
using UnityEngine;

public class CollectibleSpawner : MonoBehaviour
{
    [SerializeField] private bool randomizeSpawnType;
    [SerializeField] private float spawnInterval;

    private float timer;

    private void Update()
    {
        timer += Time.deltaTime;
    }
}
