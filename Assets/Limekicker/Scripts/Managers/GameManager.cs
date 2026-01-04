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

    void Start()
    {
        ChangeState(new CinematicState(this));
    }

    void Update()
    {
        currentState?.Update();
    }

    public void ChangeState(IGameState newState)
    {
        currentState?.Exit();
        currentState = newState;
        currentState.Enter();
    }
}
