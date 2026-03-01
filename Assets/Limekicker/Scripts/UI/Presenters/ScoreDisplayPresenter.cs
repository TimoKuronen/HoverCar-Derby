using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreDisplayPresenter : BasePresenter
{
    private readonly IScoreDisplayView view;
    private readonly IScoreManager scoreManager;
    private readonly MonoBehaviour coroutineRunner;
    private readonly Dictionary<ulong, Action<int>> scoreUpdateCallbacks = new Dictionary<ulong, Action<int>>();

    private EventBinding<PlayerSpawnedEvent> playerSpawnedBinding;
    private EventBinding<GameStateChangeEvent> gameStateChangeBinding;

    public ScoreDisplayPresenter(IScoreDisplayView view, IScoreManager scoreManager, MonoBehaviour coroutineRunner)
    {
        this.view = view;
        this.scoreManager = scoreManager;
        this.coroutineRunner = coroutineRunner;
    }

    protected override void SubscribeToModels()
    {
        scoreManager.OnPlayerAdded += HandlePlayerAdded;

        playerSpawnedBinding = new EventBinding<PlayerSpawnedEvent>(HandlePlayerSpawned);
        EventBus<PlayerSpawnedEvent>.Register(playerSpawnedBinding);

        gameStateChangeBinding = new EventBinding<GameStateChangeEvent>(HandleGameStateChange);
        EventBus<GameStateChangeEvent>.Register(gameStateChangeBinding);
    }

    protected override void UnsubscribeFromModels()
    {
        if (scoreManager != null)
        {
            scoreManager.OnPlayerAdded -= HandlePlayerAdded;
        }

        if (playerSpawnedBinding != null)
        {
            EventBus<PlayerSpawnedEvent>.Unregister(playerSpawnedBinding);
        }

        if (gameStateChangeBinding != null)
        {
            EventBus<GameStateChangeEvent>.Unregister(gameStateChangeBinding);
        }

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

    private void HandlePlayerSpawned(PlayerSpawnedEvent playerSpawnedEvent)
    {
        coroutineRunner.StartCoroutine(WaitForScoreVariable(playerSpawnedEvent));
    }

    private IEnumerator WaitForScoreVariable(PlayerSpawnedEvent playerSpawnedEvent)
    {
        var @object = playerSpawnedEvent.NetworkObject;
        bool isBot = @object.TryGetComponent<PlayerController>(out var controller) && controller.IsBot;
        ulong clientId = isBot ? @object.NetworkObjectId : @object.OwnerClientId;

        int maxAttempts = 10;
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            if (scoreManager is ScoreManager sm)
            {
                IntVariable scoreVariable = sm.GetPlayerScoreVariable(clientId);
                if (scoreVariable != null)
                {
                    Action<int> updateCallback = (score) => view.UpdatePlayerScore(clientId, score);
                    scoreVariable.OnValueChanged += updateCallback;
                    scoreUpdateCallbacks[clientId] = updateCallback;

                    string playerName = controller != null ? controller.PlayerName.Value.ToString() : "Unknown";
                    view.AddPlayer(clientId, playerName, scoreVariable.Value);
                    yield break;
                }
            }

            attempts++;
            yield return null;
        }
    }

    private void HandlePlayerAdded(PlayerData playerData)
    {
        if (scoreManager is ScoreManager sm)
        {
            IntVariable scoreVariable = sm.GetPlayerScoreVariable(playerData.ClientId);

            if (scoreVariable != null && !scoreUpdateCallbacks.ContainsKey(playerData.ClientId))
            {
                Action<int> updateCallback = (score) => view.UpdatePlayerScore(playerData.ClientId, score);
                scoreVariable.OnValueChanged += updateCallback;
                scoreUpdateCallbacks[playerData.ClientId] = updateCallback;

                view.AddPlayer(playerData.ClientId, playerData.PlayerName.Value.ToString(), scoreVariable.Value);
            }
        }
    }

    private void HandleGameStateChange(GameStateChangeEvent @event)
    {
        if (@event.NewState is RaceCompletionState)
        {
            view.MoveToCenter();
        }
        else
        {
            view.ResetToGamePosition();
        }
    }
}
