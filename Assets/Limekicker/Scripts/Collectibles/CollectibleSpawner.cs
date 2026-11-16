using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using VContainer;

/// <summary>
/// Spawns collectibles on an interval while the match is in play.
/// </summary>
public class CollectibleSpawner : MonoBehaviour
{
    private const float RayStartHeight = 50f;
    private const float RayLength = 150f;
    private const float HoverOffset = 1.5f;
    private const int MaxSampleAttempts = 100;

    [SerializeField] private CollisionCollectible[] collectiblePrefabs;
    [SerializeField] private MapData mapData;
    [SerializeField] private float spawnInterval;
    [SerializeField] private float minDistanceFromCollectibles = 10f;
    [SerializeField] private float minDistanceFromCars = 10f;

    private IGameManager gameManager;
    private CollectibleType previouslySpawnedCollectible;
    private readonly List<CollisionCollectible> activeCollectibles = new();
    private readonly List<CollisionCollectible> candidatePrefabs = new();
    private readonly List<Vector3> carPositions = new();
    private readonly List<Vector3> collectiblePositions = new();

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
        if (mapData == null || collectiblePrefabs == null || collectiblePrefabs.Length == 0)
        {
            Debug.LogError("[CollectibleSpawner] Map data and collectible prefabs are required; spawning is disabled.");
            enabled = false;
            return;
        }

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
    /// Spawns a collectible on valid ground inside the map, away from cars and other collectibles.
    /// Picks a different type than the previous spawn when more than one type exists.
    /// </summary>
    private void SpawnCollectible()
    {
        if (!TryGetSpawnPosition(out Vector3 spawnPosition))
            return;

        CollisionCollectible prefabToSpawn = PickPrefab();
        previouslySpawnedCollectible = prefabToSpawn.CollectibleEffectData.Type;

        CollisionCollectible spawnedCollectible = Instantiate(prefabToSpawn, spawnPosition, Quaternion.identity);
        spawnedCollectible.NetworkObject.Spawn(destroyWithScene: true);
        activeCollectibles.Add(spawnedCollectible);
    }

    private CollisionCollectible PickPrefab()
    {
        candidatePrefabs.Clear();
        foreach (CollisionCollectible prefab in collectiblePrefabs)
        {
            if (previouslySpawnedCollectible == CollectibleType.None ||
                prefab.CollectibleEffectData.Type != previouslySpawnedCollectible)
            {
                candidatePrefabs.Add(prefab);
            }
        }

        if (candidatePrefabs.Count == 0)
            return collectiblePrefabs[0];

        return candidatePrefabs[UnityEngine.Random.Range(0, candidatePrefabs.Count)];
    }

    /// <summary>
    /// Samples random points in the map rectangle and raycasts down. A point is accepted when the
    /// hit lies in the map's ground height band and is far enough from every car and collectible.
    /// </summary>
    private bool TryGetSpawnPosition(out Vector3 spawnPosition)
    {
        spawnPosition = default;

        carPositions.Clear();
        foreach (var player in gameManager.PlayerTracker.GetAllPlayers())
        {
            carPositions.Add(player.transform.position);
        }

        activeCollectibles.RemoveAll(collectible => collectible == null || collectible.IsProcessed);
        collectiblePositions.Clear();
        foreach (var collectible in activeCollectibles)
        {
            collectiblePositions.Add(collectible.transform.position);
        }

        for (int attempt = 0; attempt < MaxSampleAttempts; attempt++)
        {
            Vector2 flatPoint = mapData.GetRandomPoint();
            Vector3 rayOrigin = new Vector3(flatPoint.x, mapData.MaxGroundHeight + RayStartHeight, flatPoint.y);

            if (!Physics.Raycast(rayOrigin, Vector3.down, out RaycastHit hit, RayLength, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
                continue;

            if (!mapData.IsGroundHeight(hit.point.y))
                continue;

            if (IsTooClose(hit.point, carPositions, minDistanceFromCars) ||
                IsTooClose(hit.point, collectiblePositions, minDistanceFromCollectibles))
                continue;

            spawnPosition = hit.point + Vector3.up * HoverOffset;
            return true;
        }

        Debug.LogWarning($"[CollectibleSpawner] No valid spawn point found after {MaxSampleAttempts} attempts.");
        return false;
    }

    private static bool IsTooClose(Vector3 point, List<Vector3> others, float minDistance)
    {
        float minDistanceSq = minDistance * minDistance;

        for (int i = 0; i < others.Count; i++)
        {
            float dx = point.x - others[i].x;
            float dz = point.z - others[i].z;
            if (dx * dx + dz * dz < minDistanceSq)
                return true;
        }

        return false;
    }

    private void OnDestroy()
    {
        EventBus<GameStateChangeEvent>.Unregister(gameStateChangeEvent);
    }
}