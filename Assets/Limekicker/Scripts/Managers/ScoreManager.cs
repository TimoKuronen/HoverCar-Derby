using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using VContainer;

public class ScoreManager : IScoreManager, IDisposable
{
    public Dictionary<PlayerData, int> PlayerScores { get; private set; } = new Dictionary<PlayerData, int>();

    public event Action<PlayerData> OnScoreChanged;
    public event Action<PlayerData> OnPlayerAdded; // for UI

    // SOAP: Track IntVariable SOs per player for score values
    private Dictionary<ulong, IntVariable> playerScoreVariables = new Dictionary<ulong, IntVariable>();
    private Dictionary<ulong, PlayerData> playerDataByClientId = new Dictionary<ulong, PlayerData>();

    private EventBinding<PlayerSpawnedEvent> playerSpawnedEvent;
    private EventBinding<DamageDealtEvent> damageDealtEvent;
    private EventBinding<CollectibleCollectedEvent> collectibleCollectedEvent;

    [Inject]
    public void Construct()
    {
        playerSpawnedEvent = new EventBinding<PlayerSpawnedEvent>(AddPlayerToScoreBoard);
        damageDealtEvent = new EventBinding<DamageDealtEvent>(HandleDamageDealt);
        collectibleCollectedEvent = new EventBinding<CollectibleCollectedEvent>(HandleCollectibleCollected);
        EventBus<PlayerSpawnedEvent>.Register(playerSpawnedEvent);
        EventBus<DamageDealtEvent>.Register(damageDealtEvent);
        EventBus<CollectibleCollectedEvent>.Register(collectibleCollectedEvent);
    }

    public IntVariable GetPlayerScoreVariable(ulong clientId)
    {
        return playerScoreVariables.TryGetValue(clientId, out var variable) ? variable : null;
    }

    private void AddPlayerToScoreBoard(PlayerSpawnedEvent playerSpawnedEvent)
    {
        var data = playerSpawnedEvent.UserData;
        var @object = playerSpawnedEvent.NetworkObject;

        // Check if this is a bot - bots use NetworkObjectId, real players use OwnerClientId
        bool isBot = @object.TryGetComponent<PlayerController>(out var controller) && controller.IsBot;
        ulong clientId = isBot ? @object.NetworkObjectId : @object.OwnerClientId;

        PlayerData playerData = new PlayerData
        {
            PlayerName = data?.userName ?? (isBot && controller != null ? controller.PlayerName.Value : new FixedString32Bytes("Unknown")),
            ClientId = clientId,
            Points = 0
        };

        // SOAP: Create IntVariable SO for this player's score
        IntVariable scoreVariable = ScriptableObject.CreateInstance<IntVariable>();
        scoreVariable.Value = 0;
        playerScoreVariables[clientId] = scoreVariable;
        playerDataByClientId[clientId] = playerData;

        OnPlayerAdded?.Invoke(playerData);
    }

    /// <summary>
    /// Handles damage dealt events and awards points to the attacker. Searches through all players
    /// to find the attacker by matching their ID (NetworkObjectId for bots, OwnerClientId for real players).
    /// </summary>
    private void HandleDamageDealt(DamageDealtEvent @event)
    {
        if (@event.DamageAmount <= 0 || @event.AttackerClientId == ulong.MaxValue)
            return;

        NetworkObject attackerObject = null;
        
        // Find the attacker by matching IDs - bots use NetworkObjectId, real players use OwnerClientId
        foreach (var player in PlayerTracker.players.Values)
        {
            if (!player.TryGetComponent<PlayerController>(out var controller))
                continue;

            bool isBot = controller.IsBot;
            ulong playerId = isBot ? player.NetworkObjectId : player.OwnerClientId;
            
            if (playerId == @event.AttackerClientId)
            {
                attackerObject = player;
                break;
            }
        }

        if (attackerObject != null && attackerObject.TryGetComponent<PlayerController>(out var attackerController))
        {
            int pointsToAdd = Mathf.RoundToInt(@event.DamageAmount);
            IncreaseScore(attackerController, pointsToAdd);
        }
    }

    private void HandleCollectibleCollected(CollectibleCollectedEvent collectedEvent)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsServer)
            return;

        if (collectedEvent.Type != CollectibleType.Points || collectedEvent.Magnitude <= 0f)
            return;

        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(
                collectedEvent.CollectorNetworkObjectId,
                out NetworkObject collectorObject))
            return;

        if (!collectorObject.TryGetComponent<PlayerController>(out PlayerController controller))
            return;

        IncreaseScore(controller, Mathf.RoundToInt(collectedEvent.Magnitude));
    }

    /// <summary>
    /// Increases score for a player. Updates both the ScriptableObject (for UI binding) and PlayerData.
    /// </summary>
    public void IncreaseScore(PlayerController data, int scoreToAdd)
    {
        // Bots use NetworkObjectId, real players use OwnerClientId (matching collision system)
        ulong clientId = data.IsBot ? data.NetworkObjectId : data.OwnerClientId;
        
        if (playerScoreVariables.TryGetValue(clientId, out var scoreVariable))
        {
            scoreVariable.Value += scoreToAdd;
            
            // Update PlayerData
            if (playerDataByClientId.TryGetValue(clientId, out var playerData))
            {
                playerData.Points = scoreVariable.Value;
                playerDataByClientId[clientId] = playerData;
            }
            
            OnScoreChanged?.Invoke(playerDataByClientId[clientId]);
        }
    }

    public PlayerData GetLeadingPlayer()
    {
        var ranked = GetRankedPlayersByScore();
        return ranked.Count > 0 ? ranked[0] : default;
    }

    public IReadOnlyList<PlayerData> GetRankedPlayersByScore()
    {
        var list = new List<PlayerData>();
        foreach (var kvp in playerDataByClientId)
        {
            if (playerScoreVariables.TryGetValue(kvp.Key, out var scoreVar))
            {
                var data = kvp.Value;
                data.Points = scoreVar.Value;
                list.Add(data);
            }
        }
        list.Sort((a, b) => b.Points.CompareTo(a.Points));
        return list;
    }

    public void Dispose()
    {
        EventBus<PlayerSpawnedEvent>.Unregister(playerSpawnedEvent);
        EventBus<DamageDealtEvent>.Unregister(damageDealtEvent);
        EventBus<CollectibleCollectedEvent>.Unregister(collectibleCollectedEvent);
    }
}

public struct PlayerData
{
    public FixedString32Bytes PlayerName;
    public ulong ClientId;
    public int Points { get; set; }
}