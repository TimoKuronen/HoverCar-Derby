using System;
using UnityEngine;

public class StateController : MonoBehaviour
{
    [Header("Set these:")]
    [SerializeField] private State remainState;
    [SerializeField] private State startState;

    public AudioSource AudioSource;

    [Header("For Debug purposes")]
    [SerializeField] private State currentState;
    [SerializeField] private State previousState;
    public float stateTimeElapsed { get; private set; }

    protected bool isActive;
    public bool waitForAction;

    public event Action OnExitStateCalled;

    private void Update()
    {
        if (!isActive)
            return;

        currentState.UpdateState(this);
    }

    public void SetupStateMachine(bool activationValue)
    {
        isActive = activationValue;

        if (isActive)
        {
            TransitionToState(startState);
        }
    }

    public void TransitionToState(State nextState)
    {
        if (nextState != remainState)
        {
            // Set previousState only if we are not going to goToPreviousState
            if (currentState != null)
            {
                previousState = currentState;
            }

            // Transition to sub-state or parent state logic
            if (nextState.parentState != null && nextState.parentState == currentState)
            {
                currentState = nextState;
            }
            else
            {
                OnExitState();
                currentState = nextState;
            }
        }
    }

    public bool CheckIfCountDownElapsed(float duration)
    {
        stateTimeElapsed += Time.deltaTime;
        return stateTimeElapsed >= duration;
    }

    void ResetTimer()
    {
        stateTimeElapsed = 0;
    }

    public void OnExitState()
    {
        ResetTimer();

        waitForAction = false;
        OnExitStateCalled?.Invoke();
    }
}