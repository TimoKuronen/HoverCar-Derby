using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

/// <summary>
/// Touch and keyboard input service for steering and gas.
/// </summary>
public class TouchInputService : IInputService, ITickable
{
    [Header("Settings")]
    [SerializeField] private float sensitivity = 0.005f;
    [SerializeField] private float smoothing = 10f;
    [SerializeField, Range(0f, 1f)] private float steeringScreenRegion = 0.5f; // left half of screen

    private bool gasPressed;
    private float currentSteer;
    private float targetSteer;

    private Vector2 startPos;
    private int activeTouchId = -1;

    public float Steering => currentSteer;
    public bool IsGasPressed => gasPressed;

    public void SetGasPressed(bool value) => gasPressed = value;

    /// <summary>
    /// Updates input handling each frame. Routes to PC or touch input based on platform.
    /// </summary>
    public void Tick()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        HandlePCInput();
#else
        HandleTouchInput();
#endif
        SmoothSteering();
    }

    // Editor / PC builds
    private void HandlePCInput()
    {
        var mouse = Mouse.current;
        var keyboard = Keyboard.current;

        if (keyboard != null)
        {
            gasPressed = keyboard.wKey.isPressed;

            float steer = 0f;
            if (keyboard.aKey.isPressed) steer -= 1f;
            if (keyboard.dKey.isPressed) steer += 1f;

            if (steer != 0)
            {
                targetSteer = steer;
            }
            else if (!mouse?.leftButton.isPressed ?? true)
            {
                targetSteer = 0f;
            }
        }

        if (mouse == null)
            return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            var pos = mouse.position.ReadValue();

            if (pos.x < Screen.width * steeringScreenRegion)
            {
                startPos = pos;
                targetSteer = 0f;
            }
            else
            {
                // ignore drags started outside region
                startPos = Vector2.zero;
            }
        }

        if (mouse.leftButton.isPressed && startPos != Vector2.zero)
        {
            var pos = mouse.position.ReadValue();
            float deltaX = pos.x - startPos.x;
            targetSteer = Mathf.Clamp(deltaX * sensitivity, -1f, 1f);
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            targetSteer = 0f;
            startPos = Vector2.zero;
        }
    }

    // Mobile builds
    private void HandleTouchInput()
    {
        var touchscreen = Touchscreen.current;

        // If touchscreen is null but we have an active touch, reset it
        // This can happen during scene transitions or when input system is temporarily unavailable
        if (touchscreen == null)
        {
            if (activeTouchId != -1)
            {
                ResetTouchState();
            }
            return;
        }

        bool activeTouchExists = false;
        bool activeTouchEnded = false;

        if (activeTouchId != -1)
        {
            foreach (var touch in touchscreen.touches)
            {
                var phase = touch.phase.ReadValue();
                var id = touch.touchId.ReadValue();

                if (id == activeTouchId)
                {
                    activeTouchExists = true;

                    if (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                        phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                    {
                        activeTouchEnded = true;
                        break;
                    }
                }
            }

            if (activeTouchEnded)
            {
                ResetTouchState();
                return;
            }

            if (!activeTouchExists)
            {
                ResetTouchState();
            }
        }

        foreach (var touch in touchscreen.touches)
        {
            if (!touch.press.isPressed)
                continue;

            var phase = touch.phase.ReadValue();
            var pos = touch.position.ReadValue();
            var id = touch.touchId.ReadValue();

            // If this is our active touch, update it
            if (id == activeTouchId)
            {
                if (phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                    phase == UnityEngine.InputSystem.TouchPhase.Stationary ||
                    phase == UnityEngine.InputSystem.TouchPhase.Began)
                {
                    float deltaX = pos.x - startPos.x;
                    targetSteer = Mathf.Clamp(deltaX * sensitivity, -1f, 1f);
                    activeTouchExists = true;
                }
            }
            else if (phase == UnityEngine.InputSystem.TouchPhase.Began &&
                     activeTouchId == -1 &&
                     pos.x < Screen.width * steeringScreenRegion)
            {
                activeTouchId = id;
                startPos = pos;
                targetSteer = 0f;
                activeTouchExists = true;
            }
        }

        // If all touches ended, reset
        if (touchscreen.touches.Count == 0 && activeTouchId != -1)
        {
            ResetTouchState();
        }
    }

    /// <summary>Resets touch input state. Called when touch is lost or scene transitions occur.</summary>
    private void ResetTouchState()
    {
        activeTouchId = -1;
        targetSteer = 0f;
        startPos = Vector2.zero;
    }

    /// <summary>
    /// Smoothly interpolates steering input to prevent sudden direction changes.
    /// </summary>
    private void SmoothSteering()
    {
        currentSteer = Mathf.Lerp(currentSteer, targetSteer, Time.deltaTime * smoothing);
    }
}