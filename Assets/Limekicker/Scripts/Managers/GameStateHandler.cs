using System;
using UnityEngine;

public class GameStateHandler : MonoBehaviour, IGameStateService
{
    private IGameState currentGameState;

    EventBinding<GameStateChangeEvent> gameStateChangeBinding;

    void OnEnable()
    {
        gameStateChangeBinding = new EventBinding<GameStateChangeEvent>(HandleGameStateChange);
        EventBus<GameStateChangeEvent>.Register(gameStateChangeBinding);
    }

    private void HandleGameStateChange(GameStateChangeEvent @event)
    {
        throw new NotImplementedException();
    }

    public IGameState CurrentGameState
    {
        get { return currentGameState; }
        set
        {
            if (currentGameState != value)
            {
                currentGameState = value;
            }
        }
    }

    void OnDisable()
    {
        EventBus<GameStateChangeEvent>.Unregister(gameStateChangeBinding);
    }
}
