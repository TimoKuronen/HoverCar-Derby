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

    private IGameManager gameManager;
    private CollectibleType previouslySpawnedCollectible;
    private List<CollisionCollectible> activeCollectibles = new();

    private bool isSpawningActive = false;
    private float timer;

    private EventBinding<GameStateChangeEvent> gameStateChangeEvent;

    [Inject]
    public void Construct(IGameManager gameManager)
    {
        this.gameManager = gameManager;

        gameStateChangeEvent = new EventBinding<GameStateChangeEvent>(HandleGameStateChange);
    }

    public void Start()
    {
        EventBus<GameStateChangeEvent>.Register(gameStateChangeEvent);
    }

    private void HandleGameStateChange(GameStateChangeEvent @event)
    {
        isSpawningActive = @event.NewState is PlayState;
    }

    private void Update()
    {
        if (!isSpawningActive || NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        timer += Time.deltaTime;
        if (timer >= spawnInterval)
        {
            SpawnCollectible();
            timer = 0f;
        }
    }

    /// <summary>
    /// Spawns a collectible at a random position away from players and other collectibles.
    /// Ensures different type than previously spawned to add variety.
    /// </summary>
    private void SpawnCollectible()
    {
        Vector3 spawnPosition = GetPointOutOfReach();

        if (spawnPosition == Vector3.zero)
            return;

        // Raycast down to find the ground
        spawnPosition = Physics.Raycast(spawnPosition + Vector3.up * 50f, Vector3.down, out RaycastHit hitInfo, 100f) ? hitInfo.point : spawnPosition;
        spawnPosition += Vector3.up * 1.5f;

        CollisionCollectible prefabToSpawn;
        while (true)
        {
            prefabToSpawn = collectiblePrefabs[UnityEngine.Random.Range(0, collectiblePrefabs.Length)];

            if (previouslySpawnedCollectible == CollectibleType.None ||
                prefabToSpawn.CollectibleEffectData.Type != previouslySpawnedCollectible)
            {
                previouslySpawnedCollectible = prefabToSpawn.CollectibleEffectData.Type;
                break;
            }
        }

        NetworkObject spawnedCollectible = Instantiate(prefabToSpawn.NetworkObject, spawnPosition, Quaternion.identity);
        spawnedCollectible.Spawn();
        activeCollectibles.Add(spawnedCollectible.GetComponent<CollisionCollectible>());
    }

    /// <summary>
    /// Finds a random spawn position that is far enough from all players and existing collectibles.
    /// Uses rejection sampling with a maximum attempt limit.
    /// </summary>
    private Vector3 GetPointOutOfReach()
    {
        List<Vector3> existingPositions = new List<Vector3>();

        foreach (var player in gameManager.PlayerTracker.GetAllPlayers())
        {
            existingPositions.Add(player.transform.position);
        }
        activeCollectibles.RemoveAll(collectible => collectible == null);
        foreach (var collectible in activeCollectibles)
        {
            existingPositions.Add(collectible.transform.position);
        }

        Vector3 randomPoint;
        int attempts = 0;

        // Try to find a valid spawn point
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

    private void OnDestroy()
    {
        EventBus<GameStateChangeEvent>.Unregister(gameStateChangeEvent);
    }
}