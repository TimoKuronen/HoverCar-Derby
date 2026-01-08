using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IGameState
{
    void Enter();
    void Update();
    void Exit();
}

public class GameManager : MonoBehaviour
{
    [Header("References")]
    public RaceContext Context;

    private IGameState currentState;
    private IGameState previousState;

    IEnumerator Start()
    {
        // Start in Cinematic State
        // ChangeState(new CinematicState(this));

        yield return new WaitForSeconds(1);
        // For now, start directly in Play State
        Context.raceCamera.Priority = 20;
        ChangeState(new PlayState());
    }

    void Update()
    {
        currentState?.Update();
    }

    public void ChangeState(IGameState newState)
    {
        if (newState is not PauseState && previousState is not PauseState)
            previousState = currentState;

        currentState?.Exit();
        currentState = newState;
        currentState.Enter();

        EventBus<GameStateChangeEvent>.Raise(new GameStateChangeEvent { NewState = currentState });
    }

    public void ReturnToPreviousState()
    {
        ChangeState(previousState);
    }
}
