using Unity.Netcode;
using UnityEngine;

public class SimpleHoverChaseCam : MonoBehaviour
{
    #region Fields
    [Header("Settings")]
    public float distance = 8;
    public float height = 3;
    public float rotationSpeed = 5;
    public float minTiltAngle = 5, maxTiltAngle = 15, maxSpeedForTilt = 45;
    public Vector3 velocity;

    private Transform target;
    private Rigidbody targetRigidbody;
    private EventBinding<GameStateChangeEvent> gameStateChangeEvent;
    private EventBinding<PlayerSpawnedEvent> playerSpawnedEvent;
    private EventBinding<PlayerTeleportedEvent> playerTeleportedEvent;
    #endregion

    #region Properties
    public int TryAssignLocalPlayer { get; private set; }
    #endregion

    #region Unity Lifecycle
    public void Start()
    {
        gameStateChangeEvent = new EventBinding<GameStateChangeEvent>(HandleGameStateChange);
        EventBus<GameStateChangeEvent>.Register(gameStateChangeEvent);

        playerSpawnedEvent = new EventBinding<PlayerSpawnedEvent>(HandlePlayerSpawnFromManager);
        EventBus<PlayerSpawnedEvent>.Register(playerSpawnedEvent);

        playerTeleportedEvent = new EventBinding<PlayerTeleportedEvent>(HandlePlayerTeleported);
        EventBus<PlayerTeleportedEvent>.Register(playerTeleportedEvent);
    }

    void LateUpdate()
    {
        MoveCamera();
    }

    private void OnDestroy()
    {
        EventBus<GameStateChangeEvent>.Unregister(gameStateChangeEvent);
        EventBus<PlayerSpawnedEvent>.Unregister(playerSpawnedEvent);
        EventBus<PlayerTeleportedEvent>.Unregister(playerTeleportedEvent);
    }
    #endregion

    #region Public Methods
    /// <summary>
    /// Instantly snaps camera to target position and rotation. Used after teleport events.
    /// </summary>
    public void ForceUpdatePosition()
    {
        if (target == null)
            return;

        Rigidbody rb = target.GetComponent<Rigidbody>();
        float speed = rb != null ? rb.velocity.magnitude : 0f;

        Vector3 targetPosition = target.position - target.forward * distance + Vector3.up * height;

        if (speed > 2f)
        {
            targetPosition += target.forward * (speed * 0.1f);
        }

        transform.position = targetPosition;

        Vector3 lookDir = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
        Quaternion targetRotation = Quaternion.LookRotation(lookDir.normalized);
        float speedFactor = rb != null ? Mathf.Clamp01(rb.velocity.magnitude / maxSpeedForTilt) : 0f;
        float currentTiltAngle = Mathf.Lerp(minTiltAngle, maxTiltAngle, speedFactor);
        Quaternion tiltRotation = Quaternion.Euler(currentTiltAngle, targetRotation.eulerAngles.y, 0);
        transform.rotation = tiltRotation;
        velocity = Vector3.zero;
    }
    #endregion

    #region Private Methods
    private void HandleGameStateChange(GameStateChangeEvent @event)
    {
        if (@event.NewState is not PlayState)
            return;

        if (target != null)
            return;

        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
            return;

        var localPlayer = NetworkManager.Singleton.SpawnManager?.GetLocalPlayerObject();
        if (localPlayer != null && localPlayer.IsOwner)
        {
            AssignTarget(localPlayer.transform, localPlayer.GetComponent<Rigidbody>());
        }
    }

    private void HandlePlayerSpawnFromManager(PlayerSpawnedEvent playerSpawnedEvent)
    {
        if (playerSpawnedEvent.NetworkObject == null)
            return;
            
        var playerController = playerSpawnedEvent.NetworkObject.GetComponent<PlayerController>();
        if (playerController != null && playerController.IsBot)
            return;
            
        if (playerSpawnedEvent.NetworkObject.IsOwner)
        {
            AssignTarget(playerSpawnedEvent.NetworkObject.transform, playerSpawnedEvent.NetworkObject.GetComponent<Rigidbody>());
        }
    }

    private void HandlePlayerTeleported(PlayerTeleportedEvent @event)
    {
        if (@event.NetworkObject == null)
            return;

        if (target != null && @event.NetworkObject.transform == target)
        {
            ForceUpdatePosition();
        }
    }

    private void AssignTarget(Transform newTarget, Rigidbody newRigidbody)
    {
        if (newTarget == null)
            return;

        target = newTarget;
        targetRigidbody = newRigidbody != null ? newRigidbody : newTarget.GetComponent<Rigidbody>();
    }

    /// <summary>
    /// Smoothly follows the target car with dynamic positioning and rotation.
    /// Camera speed increases during sharp turns, and tilt angle increases with speed.
    /// </summary>
    private void MoveCamera()
    {
        if (!target)
            return;

        Rigidbody rb = target.GetComponent<Rigidbody>();
        float speed = rb.velocity.magnitude;

        // Base position behind and above the car
        Vector3 targetPosition = target.position - target.forward * distance + Vector3.up * height;

        // Push camera forward slightly when moving fast for better visibility
        if (speed > 2f)
        {
            targetPosition += target.forward * (speed * 0.1f);
        }

        // Adjust smoothing based on turn sharpness - faster response during sharp turns
        float turnSharpness = rb ? rb.angularVelocity.magnitude : 0f;
        float turnBoost = Mathf.Clamp01(turnSharpness / 2f);
        float smoothTime = Mathf.Lerp(0.03f, 0.1f, 1f - turnBoost);

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

        // Tilt camera down more at higher speeds
        float speedFactor = Mathf.Clamp01(speed / maxSpeedForTilt);
        float currentTiltAngle = Mathf.Lerp(minTiltAngle, maxTiltAngle, speedFactor);

        Vector3 lookDir = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
        Quaternion targetRotation = Quaternion.LookRotation(lookDir.normalized);
        Quaternion tiltRotation = Quaternion.Euler(currentTiltAngle, targetRotation.eulerAngles.y, 0);

        // Rotate faster during sharp turns
        float rotSpeed = rotationSpeed * (1f + turnBoost * 2f);
        transform.rotation = Quaternion.Slerp(transform.rotation, tiltRotation, rotSpeed * Time.deltaTime);
    }
    #endregion
}
