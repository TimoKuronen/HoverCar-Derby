using Unity.Netcode;
using UnityEngine;

public class HoverCarMover : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rig;
    [SerializeField] private CarManager carManager;

    [Header("Movement")]
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

    private IInputManager inputManager;
    private IGameStateHandler gameStateHandler;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            return;
    }

    void Start()
    {
        inputManager = DIBootstrapper.Container.Resolve<IInputManager>();
        gameStateHandler = DIBootstrapper.Container.Resolve<IGameStateHandler>();

        originalAccelerationValue = forwardAcceleration;
        originalMaxSpeed = maxSpeed;
    }

    void Update()
    {
        if (!IsOwner)
            return;

        if (gameStateHandler.GetCurrentGameState == GameState.Normal)
            GetInput();
    }

    private void FixedUpdate()
    {
        if (!IsOwner)
            return;

        rig.maxAngularVelocity = maxAngularVelocity;

        ApplyMovement();
    }

    private void GetInput()
    {
        float steer = inputManager.GetSteer();
        float gas = inputManager.GetGas();
        float brake = inputManager.GetBrake();

        // Forward / backward thrust
        float rawVertical = gas > 0 ? 1f : (brake > 0 ? -1f : 0f);

        currentThrust = Mathf.Abs(rawVertical) > inputDeadZone
            ? rawVertical * (rawVertical > 0 ? forwardAcceleration : backwardAcceleration)
            : 0f;

        // Turning
        currentTurn = Mathf.Abs(steer) > inputDeadZone
            ? steer * turnStrength
            : 0f;
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
            rig.AddForce(carManager.CarData.GetAccelerationMultiplier() * currentThrust * transform.forward, ForceMode.Acceleration);
        }

        if (currentTurn != 0)
        {
            rig.AddTorque(Vector3.up * currentTurn, ForceMode.Acceleration);
        }

        if (rig.velocity.magnitude > maxSpeed)
        {
            rig.velocity = carManager.CarData.GetMaxSpeedMultiplier() * maxSpeed * rig.velocity.normalized;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (!IsOwner)
            return;
    }
}
