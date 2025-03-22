using UnityEngine;

public class HoverCarMover : MonoBehaviour
{
    [SerializeField] private float inputDeadZone = 0.1f;
    [SerializeField] private float forwardAcceleration;
    [SerializeField] private float backwardAcceleration;
    [SerializeField] private float turnStrength = 10f;
    [SerializeField] private float maxSpeed;
    [SerializeField] private float maxAngularVelocity = 60f;

    private float currentThrust = 0.0f;
    private float currentTurn;
    private float originalAccelerationValue;
    private float originalMaxSpeed;
    private float horizontalInput;
    private float verticalInput;

    private Rigidbody rig;
    private IInputManager inputManager;

    void Start()
    {
        rig = GetComponent<Rigidbody>();

        originalAccelerationValue = forwardAcceleration;
        originalMaxSpeed = maxSpeed;
        inputManager = Services.Get<IInputManager>();
    }

    void Update()
    {
        GetInput();
    }

    private void FixedUpdate()
    {
        rig.maxAngularVelocity = maxAngularVelocity;

        ApplyMovement();
    }

    private void GetInput()
    {
        Vector2 delta;
        if (!inputManager.InputGiven)
        {
            delta = Vector2.zero;
        }
        else
            delta = inputManager.CurrentTouchPosition - inputManager.StartingTouchPosition;

        horizontalInput = Mathf.Clamp(delta.x / Screen.width, -1f, 1f);
        verticalInput = Mathf.Clamp(delta.y / Screen.height, -1f, 1f);

        currentThrust = Mathf.Abs(verticalInput) > inputDeadZone ? verticalInput * (verticalInput > 0 ? forwardAcceleration : backwardAcceleration) : 0f;
        currentTurn = Mathf.Abs(horizontalInput) > inputDeadZone ? horizontalInput * turnStrength : 0f;
    }

    public void ToggleNitroBoost(bool value, float nitroMultiplierValue, float maxSpeedMultiplier)
    {
        if (value)
        {
            forwardAcceleration *= nitroMultiplierValue;
            maxSpeed *= maxSpeedMultiplier;
        }
        else
        {
            forwardAcceleration = originalAccelerationValue;
            maxSpeed = originalMaxSpeed;
        }
    }

    private void ApplyMovement()
    {
        if (currentThrust != 0)
        {
            rig.AddForce(transform.forward * currentThrust, ForceMode.Acceleration);
        }

        if (currentTurn != 0)
        {
            rig.AddTorque(Vector3.up * currentTurn, ForceMode.Acceleration);
        }

        if (rig.velocity.magnitude > maxSpeed)
        {
            rig.velocity = rig.velocity.normalized * maxSpeed;
        }
    }
}
