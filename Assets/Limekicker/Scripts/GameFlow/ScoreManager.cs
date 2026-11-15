using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using VContainer;

/// <summary>
/// Tracks per-player scores and updates them from gameplay events.
/// </summary>
public class ScoreManager : IScoreManager, IDisposable
{
    public event Action<PlayerData> OnScoreChanged;
    public event Action<PlayerData> OnPlayerAdded;

    // Keyed by OwnerClientId for players and NetworkObjectId for bots (no owning client),
    // the same ids the damage system reports as attackers.
    private readonly ScoreLedger ledger = new ScoreLedger();
    private readonly Dictionary<ulong, FixedString32Bytes> namesByClientId = new Dictionary<ulong, FixedString32Bytes>();

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

    private void AddPlayerToScoreBoard(PlayerSpawnedEvent playerSpawnedEvent)
    {
        var data = playerSpawnedEvent.UserData;
        var @object = playerSpawnedEvent.NetworkObject;

        bool isBot = @object.TryGetComponent<PlayerController>(out var controller) && controller.IsBot;
        ulong clientId = isBot ? @object.NetworkObjectId : @object.OwnerClientId;

        if (!ledger.TryAddPlayer(clientId))
            return;

        namesByClientId[clientId] = ResolvePlayerName(data, controller, isBot);

        OnPlayerAdded?.Invoke(BuildPlayerData(clientId, 0));
    }

    /// <summary>
    /// Awards points equal to the damage dealt to the attacker.
    /// </summary>
    private void HandleDamageDealt(DamageDealtEvent @event)
    {
        if (!ScoreLedger.TryGetDamagePoints(@event.DamageAmount, @event.AttackerClientId, out int pointsToAdd))
            return;

        NetworkObject attackerObject = null;

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
    /// Increases score for a player and mirrors it to the player's networked Score.
    /// </summary>
    private void IncreaseScore(PlayerController data, int scoreToAdd)
    {
        ulong clientId = data.IsBot ? data.NetworkObjectId : data.OwnerClientId;

        if (!ledger.TryAward(clientId, scoreToAdd, out int total))
            return;

        data.Score.Value = total;

        OnScoreChanged?.Invoke(BuildPlayerData(clientId, total));
    }

    public PlayerData GetLeadingPlayer()
    {
        var ranked = GetRankedPlayersByScore();
        return ranked.Count > 0 ? ranked[0] : default;
    }

    public IReadOnlyList<PlayerData> GetRankedPlayersByScore()
    {
        var ranked = ledger.GetRanked();
        var list = new List<PlayerData>(ranked.Count);
        foreach (var entry in ranked)
            list.Add(BuildPlayerData(entry.Key, entry.Value));
        return list;
    }

    public void Dispose()
    {
        EventBus<PlayerSpawnedEvent>.Unregister(playerSpawnedEvent);
        EventBus<DamageDealtEvent>.Unregister(damageDealtEvent);
        EventBus<CollectibleCollectedEvent>.Unregister(collectibleCollectedEvent);
    }

    private PlayerData BuildPlayerData(ulong clientId, int points)
    {
        return new PlayerData
        {
            PlayerName = namesByClientId[clientId],
            ClientId = clientId,
            Points = points
        };
    }

    private static FixedString32Bytes ResolvePlayerName(UserData data, PlayerController controller, bool isBot)
    {
        if (controller != null && controller.PlayerName.Value.Length > 0)
            return controller.PlayerName.Value;

        if (data != null && !string.IsNullOrWhiteSpace(data.userName))
            return new FixedString32Bytes(data.userName);

        return isBot ? new FixedString32Bytes("Bot") : new FixedString32Bytes("Unknown");
    }
}

/// <summary>
/// Identifies a participant with display name, client id, and score.
/// </summary>
public struct PlayerData
{
    public FixedString32Bytes PlayerName;
    public ulong ClientId;
    public int Points { get; set; }
}