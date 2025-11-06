using UnityEngine;
using VContainer.Unity;

public class TouchInputService : IInputService, ITickable
{
    [SerializeField] private float sensitivity = 0.005f;
    [SerializeField] private float smoothing = 10f; // higher = smoother

    private bool gasPressed;
    private float currentSteer;
    private float targetSteer;

    private Vector2 startPos;
    private int activeTouchId = -1;

    public float Steering => currentSteer;
    public bool IsGasPressed => gasPressed;

    public void SetGasPressed(bool value)
    {
        Debug.Log($"[TouchInputService] SetGasPressed: {value}");
        gasPressed = value;
    }

    private void HandleTouchInput()
    {
        // Reset if touch lost
        if (activeTouchId != -1 && (Input.touchCount == 0 || !IsTouchActive(activeTouchId)))
        {
            activeTouchId = -1;
            targetSteer = 0f;
            return;
        }

        // Find or update touch
        foreach (var touch in Input.touches)
        {
            if (touch.phase == TouchPhase.Began && touch.position.x < Screen.width * 0.5f)
            {
                activeTouchId = touch.fingerId;
                startPos = touch.position;
                targetSteer = 0f;
            }

            if (touch.fingerId == activeTouchId)
            {
                if (touch.phase == TouchPhase.Moved || touch.phase == TouchPhase.Stationary)
                {
                    float deltaX = touch.position.x - startPos.x;
                    targetSteer = Mathf.Clamp(deltaX * sensitivity, -1f, 1f);
                }
                else if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled)
                {
                    activeTouchId = -1;
                    targetSteer = 0f;
                }
            }
        }
    }

    private void SmoothSteering()
    {
        currentSteer = Mathf.Lerp(currentSteer, targetSteer, Time.deltaTime * smoothing);
    }

    private bool IsTouchActive(int id)
    {
        foreach (var t in Input.touches)
            if (t.fingerId == id) return true;
        return false;
    }

    public void Tick()
    {
        HandleTouchInput();
        SmoothSteering();
    }
}
