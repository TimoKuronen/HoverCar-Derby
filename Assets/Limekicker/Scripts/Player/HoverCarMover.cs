using Unity.Netcode;
using UnityEngine;
using VContainer;

/// <summary>
/// Applies thrust and turn forces from input to the hover car rigidbody.
/// </summary>
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

    public override void OnNetworkSpawn()
    {
        isBot = GetComponent<BotPlayerController>() != null;

        if (!IsOwner && !isBot)
        {
            enabled = false;
            return;
        }

        gameStateChangeEvent = new EventBinding<GameStateChangeEvent>(HandleGameStateChange);
        EventBus<GameStateChangeEvent>.Register(gameStateChangeEvent);
        SyncGameplayReady();

        if (!isBot && IsOwner && inputService == null)
            TryConstructFromContainer();
    }

    public override void OnNetworkDespawn()
    {
        EventBus<GameStateChangeEvent>.Unregister(gameStateChangeEvent);
        base.OnNetworkDespawn();
    }

    private void Start()
    {
        originalAccelerationValue = forwardAcceleration;
        originalMaxSpeed = maxSpeed;
        SyncGameplayReady();

        if (!isBot && IsOwner && inputService == null)
            TryConstructFromContainer();
    }

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

    private void TryConstructFromContainer()
    {
        var bootstrapScope = FindFirstObjectByType<BootstrapLifetimeScope>();
        if (bootstrapScope != null)
        {
            try
            {
                var container = bootstrapScope.Container;
                Construct(container.Resolve<IInputService>());
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

    private void SyncGameplayReady()
    {
        GameManager gameManager = FindFirstObjectByType<GameManager>();
        isReady = gameManager != null && gameManager.CurrentGameState is PlayState;
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

    private void ApplyMovement()
    {
        if (carManager.DamageManager != null && carManager.DamageManager.IsDestroyed)
            return;

        currentThrust = inputService.IsGasPressed ? forwardAcceleration : 0f;
        if (currentThrust != 0)
            rig.AddForce(carManager.CarData.GetAccelerationMultiplier() * currentThrust * transform.forward, ForceMode.Acceleration);

        currentTurn = inputService.Steering * turnStrength;
        if (Mathf.Abs(currentTurn) > 0.01f)
            rig.AddTorque(Vector3.up * currentTurn, ForceMode.Acceleration);

        if (rig.velocity.magnitude > maxSpeed)
            rig.velocity = carManager.CarData.GetMaxSpeedMultiplier() * maxSpeed * rig.velocity.normalized;
    }
}
