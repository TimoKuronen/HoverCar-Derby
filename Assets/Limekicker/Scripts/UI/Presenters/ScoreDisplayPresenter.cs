using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;

public class ScoreDisplayPresenter : BasePresenter
{
    private readonly IScoreDisplayView view;
    private readonly IGameManager gameManager;
    private readonly HashSet<ulong> registeredClientIds = new();
    private readonly Dictionary<ulong, PlayerController> trackedControllers = new();
    private readonly Dictionary<ulong, NetworkVariable<int>.OnValueChangedDelegate> scoreChangedCallbacks = new();
    private readonly Dictionary<ulong, NetworkVariable<FixedString32Bytes>.OnValueChangedDelegate> nameChangedCallbacks = new();

    private EventBinding<PlayerSpawnedEvent> playerSpawnedBinding;
    private EventBinding<PlayerRemovedEvent> playerRemovedBinding;
    private EventBinding<GameStateChangeEvent> gameStateChangeBinding;

    public ScoreDisplayPresenter(IScoreDisplayView view, IGameManager gameManager)
    {
        this.view = view;
        this.gameManager = gameManager;
    }

    protected override void SubscribeToModels()
    {
        playerSpawnedBinding = new EventBinding<PlayerSpawnedEvent>(HandlePlayerSpawned);
        EventBus<PlayerSpawnedEvent>.Register(playerSpawnedBinding);

        playerRemovedBinding = new EventBinding<PlayerRemovedEvent>(HandlePlayerRemoved);
        EventBus<PlayerRemovedEvent>.Register(playerRemovedBinding);

        gameStateChangeBinding = new EventBinding<GameStateChangeEvent>(HandleGameStateChange);
        EventBus<GameStateChangeEvent>.Register(gameStateChangeBinding);

        RegisterTrackedPlayers();
    }

    protected override void UnsubscribeFromModels()
    {
        if (playerSpawnedBinding != null)
            EventBus<PlayerSpawnedEvent>.Unregister(playerSpawnedBinding);

        if (playerRemovedBinding != null)
            EventBus<PlayerRemovedEvent>.Unregister(playerRemovedBinding);

        if (gameStateChangeBinding != null)
            EventBus<GameStateChangeEvent>.Unregister(gameStateChangeBinding);

        foreach (KeyValuePair<ulong, PlayerController> entry in trackedControllers)
            UnregisterControllerCallbacks(entry.Key, entry.Value);

        trackedControllers.Clear();
        registeredClientIds.Clear();
        scoreChangedCallbacks.Clear();
        nameChangedCallbacks.Clear();
    }

    private void RegisterTrackedPlayers()
    {
        if (gameManager?.PlayerTracker == null)
            return;

        foreach (NetworkObject playerObject in gameManager.PlayerTracker.GetAllPlayers())
        {
            if (playerObject != null && playerObject.TryGetComponent(out PlayerController controller))
                RegisterPlayerController(controller);
        }
    }

    private void HandlePlayerSpawned(PlayerSpawnedEvent playerSpawnedEvent)
    {
        if (playerSpawnedEvent.NetworkObject != null &&
            playerSpawnedEvent.NetworkObject.TryGetComponent(out PlayerController controller))
        {
            RegisterPlayerController(controller);
        }
    }

    private void HandlePlayerRemoved(PlayerRemovedEvent playerRemovedEvent)
    {
        UnregisterPlayer(playerRemovedEvent.ClientId);
    }

    private void RegisterPlayerController(PlayerController controller)
    {
        if (controller == null || !controller.IsSpawned)
            return;

        ulong clientId = controller.IsBot ? controller.NetworkObjectId : controller.OwnerClientId;
        if (!registeredClientIds.Add(clientId))
            return;

        trackedControllers[clientId] = controller;

        NetworkVariable<int>.OnValueChangedDelegate scoreCallback = (_, newScore) =>
            view.UpdatePlayerScore(clientId, newScore);
        NetworkVariable<FixedString32Bytes>.OnValueChangedDelegate nameCallback = (_, newName) =>
            view.AddPlayer(clientId, newName.ToString(), controller.Score.Value);

        scoreChangedCallbacks[clientId] = scoreCallback;
        nameChangedCallbacks[clientId] = nameCallback;
        controller.Score.OnValueChanged += scoreCallback;
        controller.PlayerName.OnValueChanged += nameCallback;

        view.AddPlayer(clientId, controller.PlayerName.Value.ToString(), controller.Score.Value);
    }

    private void UnregisterPlayer(ulong clientId)
    {
        if (!registeredClientIds.Remove(clientId))
            return;

        if (trackedControllers.TryGetValue(clientId, out PlayerController controller))
        {
            UnregisterControllerCallbacks(clientId, controller);
            trackedControllers.Remove(clientId);
        }

        scoreChangedCallbacks.Remove(clientId);
        nameChangedCallbacks.Remove(clientId);
        view.RemovePlayer(clientId);
    }

    private void UnregisterControllerCallbacks(ulong clientId, PlayerController controller)
    {
        if (controller == null)
            return;

        if (scoreChangedCallbacks.TryGetValue(clientId, out NetworkVariable<int>.OnValueChangedDelegate scoreCallback))
            controller.Score.OnValueChanged -= scoreCallback;

        if (nameChangedCallbacks.TryGetValue(clientId, out NetworkVariable<FixedString32Bytes>.OnValueChangedDelegate nameCallback))
            controller.PlayerName.OnValueChanged -= nameCallback;
    }

    private void HandleGameStateChange(GameStateChangeEvent @event)
    {
        if (@event.NewState is not RaceCompletionState)
            view.ResetToGamePosition();
    }
}
