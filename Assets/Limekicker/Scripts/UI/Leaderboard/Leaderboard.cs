using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Leaderboard : NetworkBehaviour
{
    [SerializeField] private Transform leaderboardEntityHolder;
    [SerializeField] private LeaderboardEntity leaderboardEntityPrefab;
    [SerializeField] private int entitiesToDispaly = 6;

    private NetworkList<LeaderboardEntityState> leaderboardEntities;
    private List<LeaderboardEntity> leaderboardDisplays = new List<LeaderboardEntity>();

    private void Awake()
    {
        leaderboardEntities = new NetworkList<LeaderboardEntityState>();
    }

    private void HandlePlayerSpawned(PlayerController player)
    {
        // Check if NetworkList is still valid (NetworkBehaviour might be destroyed during shutdown)
        if (leaderboardEntities == null || !IsSpawned || player == null)
            return;

        try
        {
            var newEntry = new LeaderboardEntityState
            {
                ClientId = player.OwnerClientId,
                PlayerName = player.PlayerName.Value,
                Cash = 0
            };

            leaderboardEntities.Add(newEntry);
        }
        catch (System.Exception e)
        {
            // NetworkList might be destroyed during shutdown - this is expected
            Debug.LogWarning($"[Leaderboard] Failed to add player to leaderboard (expected during shutdown): {e.Message}");
        }

        // Subscribe to cash change event
    }

    private void HandlePlayerDespanwed(PlayerController player)
    {
        // Check if NetworkList is still valid (NetworkBehaviour might be destroyed during shutdown)
        if (leaderboardEntities == null || !IsSpawned)
            return;

        try
        {
            foreach (var entry in leaderboardEntities)
            {
                if (entry.ClientId == player.OwnerClientId)
                {
                    leaderboardEntities.Remove(entry);
                    break;
                }
            }
        }
        catch (System.Exception e)
        {
            // NetworkList might be destroyed during shutdown - this is expected
            Debug.LogWarning($"[Leaderboard] Failed to remove player from leaderboard (expected during shutdown): {e.Message}");
        }

        // Unsubscribe from cash change event
    }

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            leaderboardEntities.OnListChanged += HandleLeaderboardEntitiesChanged;
            foreach (var entity in leaderboardEntities)
            {
                HandleLeaderboardEntitiesChanged(new NetworkListEvent<LeaderboardEntityState>
                {
                    Type = NetworkListEvent<LeaderboardEntityState>.EventType.Add,
                    Value = entity
                });
            }
        }

        if (!IsServer)
            return;

        PlayerController[] players = FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach (var player in players)
        {
            HandlePlayerSpawned(player);
        }

        //PlayerController.OnPlayerSpawned += HandlePlayerSpawned;
        //PlayerController.OnPlayerDespawned += HandlePlayerDespanwed;
    }

    private void HandleLeaderboardEntitiesChanged(NetworkListEvent<LeaderboardEntityState> changeEvent)
    {
        // Check if object is still valid and scene is loaded
        if (this == null || !gameObject.scene.isLoaded || leaderboardEntityHolder == null)
            return;

        try
        {
            switch (changeEvent.Type)
            {
                case NetworkListEvent<LeaderboardEntityState>.EventType.Add:
                    if (leaderboardDisplays.Any(x => x.ClientId == changeEvent.Value.ClientId))
                    {
                        if (leaderboardEntityPrefab != null && leaderboardEntityHolder != null)
                        {
                            var newEntity = Instantiate(leaderboardEntityPrefab, leaderboardEntityHolder);
                            newEntity.Initialise(
                                changeEvent.Value.ClientId,
                                changeEvent.Value.PlayerName,
                                changeEvent.Value.Cash);
                            leaderboardDisplays.Add(newEntity);
                        }
                    }
                    break;

                case NetworkListEvent<LeaderboardEntityState>.EventType.Remove:
                    var entityToRemove = leaderboardDisplays.FirstOrDefault(x => x.ClientId == changeEvent.Value.ClientId);
                    if (entityToRemove != null)
                    {
                        entityToRemove.transform.SetParent(null);
                        Destroy(entityToRemove.gameObject);
                        leaderboardDisplays.Remove(entityToRemove);
                    }
                    break;
                case NetworkListEvent<LeaderboardEntityState>.EventType.Value:
                    var entityToUpdate = leaderboardDisplays.FirstOrDefault(x => x.ClientId == changeEvent.Value.ClientId);
                    if (entityToUpdate != null)
                    {
                        entityToUpdate.UpdatePoints(changeEvent.Value.Cash);
                    }
                    break;
            }

            leaderboardDisplays.Sort((x, y) => y.Points.CompareTo(x.Points));

            for (int i = 0; i < leaderboardDisplays.Count; i++)
            {
                if (leaderboardDisplays[i] != null && leaderboardDisplays[i].transform != null)
                {
                    leaderboardDisplays[i].transform.SetSiblingIndex(i);
                    leaderboardDisplays[i].UpdateText();
                    leaderboardDisplays[i].gameObject.SetActive(i <= entitiesToDispaly);
                }
            }
        }
        catch (System.Exception e)
        {
            // During shutdown, objects might be destroyed - this is expected
            Debug.LogWarning($"[Leaderboard] Exception during leaderboard update (expected during shutdown): {e.Message}");
        }

        // Check if NetworkManager is still valid (might be null during shutdown)
        if (NetworkManager.Singleton != null)
        {
            LeaderboardEntity myDisplay = leaderboardDisplays.FirstOrDefault(x => x.ClientId == NetworkManager.Singleton.LocalClientId);

            if (myDisplay != null && leaderboardEntityHolder != null)
            {
                if (myDisplay.transform.GetSiblingIndex() >= entitiesToDispaly && leaderboardEntityHolder.childCount > entitiesToDispaly - 1)
                {
                    leaderboardEntityHolder.GetChild(entitiesToDispaly - 1).gameObject.SetActive(false);
                    myDisplay.gameObject.SetActive(true);
                }
            }
        }
    }

    public override void OnNetworkDespawn()
    {
        if (IsClient)
        {
            leaderboardEntities.OnListChanged -= HandleLeaderboardEntitiesChanged;
        }

        if (!IsServer)
            return;

        //PlayerController.OnPlayerSpawned -= HandlePlayerSpawned;
        //PlayerController.OnPlayerDespawned -= HandlePlayerDespanwed;
    }

    /// <summary>
    /// To-do: Implement cash change handling
    /// </summary>
    /// <param name="clientId"></param>
    /// <param name="newCash"></param>
    private void HandleCashChanged(ulong clientId, int newCash)
    {
        for (int i = 0; i < leaderboardEntities.Count; i++)
        {
            if (leaderboardEntities[i].ClientId == clientId)
            {
                leaderboardEntities[i] = new LeaderboardEntityState
                {
                    ClientId = leaderboardEntities[i].ClientId,
                    PlayerName = leaderboardEntities[i].PlayerName,
                    Cash = newCash
                };

                break;
            }
        }
    }
}