using Unity.Netcode;
using UnityEngine;
using VContainer;

public class HoverCarMover : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private Rigidbody rig;
    [SerializeField] private CarManager carManager;

    [Header("Movement")]
    [SerializeField] private float forwardAcceleration = 30f;
    [SerializeField] private float turnStrength = 10f;
    [SerializeField] private float maxSpeed = 40f;
    [SerializeField] private float maxAngularVelocity = 60f;

    private IInputService inputService;
    private EventBinding<GameStateChangeEvent> gameStateChangeEvent;

    private float currentThrust;
    private float currentTurn;
    private float originalAccelerationValue;
    private float originalMaxSpeed;
    private bool isReady;
    private bool isBot;

    #region Public Methods
    public void Construct(IInputService inputService)
    {
        this.inputService = inputService;
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
    #endregion

    #region Network Lifecycle
    public override void OnNetworkSpawn()
    {
        isBot = GetComponent<BotPlayerController>() != null;

        if (!IsOwner && !isBot)
        {
            enabled = false;
            return;
        }

        if (!isBot && IsOwner && inputService == null)
        {
            TryConstructFromContainer();
        }
    }

    public override void OnNetworkDespawn()
    {
        EventBus<GameStateChangeEvent>.Unregister(gameStateChangeEvent);
        base.OnNetworkDespawn();
    }
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        originalAccelerationValue = forwardAcceleration;
        originalMaxSpeed = maxSpeed;

        gameStateChangeEvent = new EventBinding<GameStateChangeEvent>(HandleGameStateChange);
        EventBus<GameStateChangeEvent>.Register(gameStateChangeEvent);
        
        var gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null && gameManager.CurrentGameState is PlayState)
        {
            isReady = true;
        }
        
        if (!isBot && IsOwner && inputService == null)
        {
            TryConstructFromContainer();
        }
    }
    #endregion

    #region Private Methods
    private void TryConstructFromContainer()
    {
        var bootstrapScope = FindFirstObjectByType<BootstrapLifetimeScope>();
        if (bootstrapScope != null)
        {
            try
            {
                var container = bootstrapScope.Container;
                var inputService = container.Resolve<IInputService>();
                Construct(inputService);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HoverCarMover] Failed to resolve IInputService from container: {ex.Message}");
            }
        }
    }

    private void HandleGameStateChange(GameStateChangeEvent @event)
    {
        isReady = @event.NewState is PlayState;
    }

    private void FixedUpdate()
    {
        if ((!IsOwner && !isBot) || !isReady || inputService == null)
            return;

        if (rig == null || carManager == null || carManager.CarData == null)
            return;

        rig.maxAngularVelocity = maxAngularVelocity;
        ApplyMovement();
    }

    /// <summary>
    /// Applies movement forces based on input. Uses acceleration for forward movement and torque for turning.
    /// Clamps velocity to max speed, accounting for car data multipliers.
    /// </summary>
    private void ApplyMovement()
    {
        if (carManager.DamageManager != null && carManager.DamageManager.IsDestroyed)
            return;

        // Apply forward acceleration
        currentThrust = inputService.IsGasPressed ? forwardAcceleration : 0f;
        if (currentThrust != 0)
        {
            rig.AddForce(carManager.CarData.GetAccelerationMultiplier() * currentThrust * transform.forward, ForceMode.Acceleration);
        }

        // Apply turning torque
        currentTurn = inputService.Steering * turnStrength;
        if (Mathf.Abs(currentTurn) > 0.01f)
        {
            rig.AddTorque(Vector3.up * currentTurn, ForceMode.Acceleration);
        }

        // Clamp speed to max, accounting for car-specific multipliers
        if (rig.velocity.magnitude > maxSpeed)
        {
            rig.velocity = carManager.CarData.GetMaxSpeedMultiplier() * maxSpeed * rig.velocity.normalized;
        }
    }
    #endregion
}
