using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class Leaderboard : NetworkBehaviour
{
    [SerializeField] private Transform leaderboardEntityHolder;
    [SerializeField] private LeaderboardEntity leaderboardEntityPrefab;

    private NetworkList<LeaderboardEntityState> leaderboardEntities;
    private List<LeaderboardEntity> leaderboardDisplays = new List<LeaderboardEntity>();

    private void Awake()
    {
        leaderboardEntities = new NetworkList<LeaderboardEntityState>();
    }

    private void HandlePlayerSpawned(PlayerController player)
    {
        var newEntry = new LeaderboardEntityState
        {
            ClientId = player.OwnerClientId,
            PlayerName = player.PlayerName.Value,
            Cash = 0
        };

        leaderboardEntities.Add(newEntry);

        // Subscribe to cash change event
    }

    private void HandlePlayerDespanwed(PlayerController player)
    {
        foreach (var entry in leaderboardEntities)
        {
            if (entry.ClientId == player.OwnerClientId)
            {
                leaderboardEntities.Remove(entry);
                break;
            }
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

        PlayerController.OnPlayerSpawned += HandlePlayerSpawned;
        PlayerController.OnPlayerDespawned += HandlePlayerDespanwed;
    }

    private void HandleLeaderboardEntitiesChanged(NetworkListEvent<LeaderboardEntityState> changeEvent)
    {
        switch (changeEvent.Type)
        {
            case NetworkListEvent<LeaderboardEntityState>.EventType.Add:
                if (leaderboardDisplays.Any(x => x.ClientId == changeEvent.Value.ClientId))
                {
                    var newEntity = Instantiate(leaderboardEntityPrefab, leaderboardEntityHolder);
                    newEntity.Initialise(
                        changeEvent.Value.ClientId,
                        changeEvent.Value.PlayerName,
                        changeEvent.Value.Cash);
                    leaderboardDisplays.Add(newEntity);
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
                    entityToUpdate.UpdateCash(changeEvent.Value.Cash);
                }
                break;
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

        PlayerController.OnPlayerSpawned -= HandlePlayerSpawned;
        PlayerController.OnPlayerDespawned -= HandlePlayerDespanwed;
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