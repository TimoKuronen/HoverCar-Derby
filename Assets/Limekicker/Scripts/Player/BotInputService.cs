using UnityEngine;

/// <summary>
/// Simple AI input service for bot/puppet players.
/// Provides basic movement patterns for testing collisions.
/// </summary>
public class BotInputService : IInputService
{
    private float steeringInput = 0f;
    private bool gasPressed = false;
    private float behaviorTimer = 0f;
    private float behaviorChangeInterval = 2f; // Change behavior every 2 seconds

    // Simple state machine for bot behavior
    private enum BotState
    {
        DrivingForward,
        TurningLeft,
        TurningRight,
        Braking
    }

    private BotState currentState = BotState.DrivingForward;

    public float Steering => steeringInput;
    public bool IsGasPressed => gasPressed;
    public bool IsBrakePressed => false; // Bots don't brake for now

    public void SetGasPressed(bool value)
    {
        gasPressed = value;
    }

    public void Tick()
    {
        behaviorTimer += Time.deltaTime;

        if (behaviorTimer >= behaviorChangeInterval)
        {
            behaviorTimer = 0f;
            ChangeBehavior();
        }
    }

    private void ChangeBehavior()
    {
        // Randomly change behavior
        int random = Random.Range(0, 4);
        currentState = (BotState)random;

        // Randomize behavior change interval for more natural movement
        behaviorChangeInterval = Random.Range(1.5f, 3f);
    }

    private void UpdateInputs()
    {
        switch (currentState)
        {
            case BotState.DrivingForward:
                steeringInput = 0f;
                gasPressed = true;
                break;

            case BotState.TurningLeft:
                steeringInput = -1f;
                gasPressed = true;
                break;

            case BotState.TurningRight:
                steeringInput = 1f;
                gasPressed = true;
                break;

            case BotState.Braking:
                steeringInput = 0f;
                gasPressed = false;
                break;
        }
    }

    public void Reset()
    {
        behaviorTimer = 0f;
        currentState = BotState.DrivingForward;
        steeringInput = 0f;
        gasPressed = true;
    }
}

