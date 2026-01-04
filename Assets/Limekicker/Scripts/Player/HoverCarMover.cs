using System;
using System.Collections;
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

    private float currentThrust;
    private float currentTurn;
    private float originalAccelerationValue;
    private float originalMaxSpeed;

    private IInputService inputService;
    private bool isReady = false;
    private bool isBot = false;

    private EventBinding<GameStateChangeEvent> gameStateChangeEvent;

    public void Construct(IInputService inputService)
    {
        if (inputService == null)
        {
            Debug.LogError("[HoverCarMover] Cannot construct with null inputService!");
            return;
        }

        // Prevent double construction
        if (this.inputService != null)
        {
            Debug.LogWarning("[HoverCarMover] Already constructed, skipping duplicate construction.");
            return;
        }

        this.inputService = inputService;
    }

    public override void OnNetworkSpawn()
    {
        // Allow bots to be enabled (they're server-controlled)
        isBot = GetComponent<BotPlayerController>() != null;

        if (!IsOwner && !isBot)
        {
            enabled = false;
            return;
        }

        // On pure clients (not server/host), construct using VContainer when player spawns
        // Server-side construction happens in PlayerSpawnManager.HandleUserJoined()
        // Hosts will also get constructed server-side, so we check if already constructed
        if (!IsServer && inputService == null)
        {
            TryConstructFromContainer();
        }
        else if (IsServer && inputService == null && !isBot)
        {
            // On server/host, if Construct wasn't called yet, it will be called in HandleUserJoined
            // But just in case, log a warning (bots are handled separately)
            Debug.LogWarning("[HoverCarMover] On server but inputService is null. Will be constructed in PlayerSpawnManager.");
        }
    }

    /// <summary>Attempts to resolve IInputService from VContainer and construct this component.</summary>
    private void TryConstructFromContainer()
    {
        // Try to find BootstrapLifetimeScope which has IInputService registered
        var bootstrapScope = FindFirstObjectByType<BootstrapLifetimeScope>();
        if (bootstrapScope != null)
        {
            try
            {
                var container = bootstrapScope.Container;
                var inputService = container.Resolve<IInputService>();
                Construct(inputService);
                Debug.Log("[HoverCarMover] Successfully constructed on client via VContainer.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HoverCarMover] Failed to resolve IInputService from container: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning("[HoverCarMover] BootstrapLifetimeScope not found. Cannot construct on client.");
        }
    }

    private void Start()
    {
        originalAccelerationValue = forwardAcceleration;
        originalMaxSpeed = maxSpeed;

        gameStateChangeEvent = new EventBinding<GameStateChangeEvent>(HandleGameStateChange);
        EventBus<GameStateChangeEvent>.Register(gameStateChangeEvent);
    }

    private void HandleGameStateChange(GameStateChangeEvent @event)
    {
        switch (@event.NewState)
        {
            case PlayState:
                rig.isKinematic = false;
                isReady = true;
                break;
            default:
                rig.isKinematic = true;
                isReady = false;
                break;
        }
    }

    private void FixedUpdate()
    {
        // Only the owning client (for player cars) or the server (for bots)
        // should drive physics. Also ensure inputService is present so we
        // don't apply movement with a null input source.
        if ((!IsOwner && !isBot) || !isReady || inputService == null)
            return;

        rig.maxAngularVelocity = maxAngularVelocity;
        ApplyMovement();
    }

    private void ApplyMovement()
    {
        // Thrust
        currentThrust = inputService.IsGasPressed ? forwardAcceleration : 0f;
        if (currentThrust != 0)
        {
            rig.AddForce(carManager.CarData.GetAccelerationMultiplier() * currentThrust * transform.forward, ForceMode.Acceleration);
        }

        // Turning
        currentTurn = inputService.Steering * turnStrength;
        if (Mathf.Abs(currentTurn) > 0.01f)
        {
            rig.AddTorque(Vector3.up * currentTurn, ForceMode.Acceleration);
        }

        // Speed limit
        if (rig.velocity.magnitude > maxSpeed)
        {
            rig.velocity = carManager.CarData.GetMaxSpeedMultiplier() * maxSpeed * rig.velocity.normalized;
        }
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

    public override void OnNetworkDespawn()
    {
        EventBus<GameStateChangeEvent>.Unregister(gameStateChangeEvent);
        base.OnNetworkDespawn();
    } 
}
