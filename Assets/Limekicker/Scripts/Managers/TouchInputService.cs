using UnityEngine;
using UnityEngine.InputSystem; // New Input System namespace

public class TouchInputService : IInputService
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

    public void Tick()
    {
#if UNITY_EDITOR || UNITY_STANDALONE
        HandleMouseInput();
#else
        HandleTouchInput();
#endif
        SmoothSteering();
    }

    // Editor / PC builds
    private void HandleMouseInput()
    {
        var mouse = Mouse.current;
        if (mouse == null) 
            return;

        if (mouse.leftButton.wasPressedThisFrame)
        {
            var pos = mouse.position.ReadValue();

            // Only register steering if pressed in steering region
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
        if (touchscreen == null) 
            return;

        foreach (var touch in touchscreen.touches)
        {
            if (!touch.press.isPressed) 
                continue;

            var phase = touch.phase.ReadValue();
            var pos = touch.position.ReadValue();
            var id = touch.touchId.ReadValue();

            // Start a new steering touch only if in steering region
            if (phase == UnityEngine.InputSystem.TouchPhase.Began &&
                activeTouchId == -1 &&
                pos.x < Screen.width * steeringScreenRegion)
            {
                activeTouchId = id;
                startPos = pos;
                targetSteer = 0f;
            }

            // Update active steering touch
            if (id == activeTouchId)
            {
                if (phase == UnityEngine.InputSystem.TouchPhase.Moved ||
                    phase == UnityEngine.InputSystem.TouchPhase.Stationary)
                {
                    float deltaX = pos.x - startPos.x;
                    targetSteer = Mathf.Clamp(deltaX * sensitivity, -1f, 1f);
                }
                else if (phase == UnityEngine.InputSystem.TouchPhase.Ended ||
                         phase == UnityEngine.InputSystem.TouchPhase.Canceled)
                {
                    activeTouchId = -1;
                    targetSteer = 0f;
                }
            }
        }

        // If all touches ended
        if (touchscreen.touches.Count == 0 && activeTouchId != -1)
        {
            activeTouchId = -1;
            targetSteer = 0f;
        }
    }

    private void SmoothSteering()
    {
        currentSteer = Mathf.Lerp(currentSteer, targetSteer, Time.deltaTime * smoothing);
    }
}
