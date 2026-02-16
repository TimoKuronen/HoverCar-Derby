using System;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class ScoreDisplay : MonoBehaviour
{
    [SerializeField] private LeaderboardEntity leaderboardEntityPrefab;
    [SerializeField] private Transform container;

    private IScoreManager scoreManager;
    private Dictionary<ulong, LeaderboardEntity> playerScores = new Dictionary<ulong, LeaderboardEntity>();
    private Dictionary<ulong, Action<int>> scoreUpdateCallbacks = new Dictionary<ulong, Action<int>>();
    private EventBinding<PlayerSpawnedEvent> playerSpawnedBinding;

    [Inject]
    public void Construct(IScoreManager scoreManager)
    {
        this.scoreManager = scoreManager;
    }

    private void Start()
    {
        // Listen to EventBus for player added (events)
        playerSpawnedBinding = new EventBinding<PlayerSpawnedEvent>(HandlePlayerSpawned);
        EventBus<PlayerSpawnedEvent>.Register(playerSpawnedBinding);
    }

    private void HandlePlayerSpawned(PlayerSpawnedEvent playerSpawnedEvent)
    {
        var @object = playerSpawnedEvent.NetworkObject;
        bool isBot = @object.TryGetComponent<PlayerController>(out var controller) && controller.IsBot;
        ulong clientId = isBot ? @object.NetworkObjectId : @object.OwnerClientId;

        // Get player data from ScoreManager
        if (scoreManager is ScoreManager sm)
        {
            IntVariable scoreVariable = sm.GetPlayerScoreVariable(clientId);
            if (scoreVariable != null)
            {
                // SOAP: Subscribe to IntVariable changes for score updates
                Action<int> updateCallback = (score) => UpdateScore(clientId, score);
                scoreVariable.OnValueChanged += updateCallback;
                scoreUpdateCallbacks[clientId] = updateCallback;

                // Add player to display
                string playerName = controller != null ? controller.PlayerName.Value.ToString() : "Unknown";
                AddPlayer(clientId, playerName, scoreVariable.Value);
            }
        }
    }

    private void AddPlayer(ulong clientId, string playerName, int initialScore)
    {
        if (playerScores.ContainsKey(clientId))
        {
            playerScores[clientId].Initialise(clientId, playerName, initialScore);
            return;
        }
        
        playerScores.Add(clientId, Instantiate(leaderboardEntityPrefab, container));
        playerScores[clientId].Initialise(clientId, playerName, initialScore);
    }

    private void UpdateScore(ulong clientId, int newScore)
    {
        if (playerScores.TryGetValue(clientId, out var entity))
        {
            entity.UpdatePoints(newScore);
        }
    }

    private void OnDestroy()
    {
        if (playerSpawnedBinding != null)
        {
            EventBus<PlayerSpawnedEvent>.Unregister(playerSpawnedBinding);
        }

        // Unsubscribe from all IntVariable changes
        if (scoreManager is ScoreManager sm)
        {
            foreach (var kvp in scoreUpdateCallbacks)
            {
                IntVariable scoreVariable = sm.GetPlayerScoreVariable(kvp.Key);
                if (scoreVariable != null)
                {
                    scoreVariable.OnValueChanged -= kvp.Value;
                }
            }
        }
        scoreUpdateCallbacks.Clear();
    }
}