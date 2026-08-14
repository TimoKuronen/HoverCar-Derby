using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class SimpleHoverChaseCam : MonoBehaviour
{
    private const int RaceCameraPlayPriority = 20;

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

        TryAssignLocalPlayerTarget("Start");
    }

    void LateUpdate()
    {
        if (target == null)
            return;

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
            targetPosition += target.forward * (speed * 0.1f);

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
        if (@event.NewState is CountdownState or PlayState)
            EnsureRaceCameraPriority();

        if (@event.NewState is PlayState)
            TryAssignLocalPlayerTarget("PlayState");
    }

    private void HandlePlayerSpawnFromManager(PlayerSpawnedEvent playerSpawnedEvent)
    {
        if (playerSpawnedEvent.NetworkObject == null)
            return;

        var playerController = playerSpawnedEvent.NetworkObject.GetComponent<PlayerController>();
        if (playerController != null && playerController.IsBot)
            return;

        if (playerSpawnedEvent.NetworkObject.IsOwner)
            AssignTarget(playerSpawnedEvent.NetworkObject.transform, playerSpawnedEvent.NetworkObject.GetComponent<Rigidbody>(), "PlayerSpawned");
    }

    private void HandlePlayerTeleported(PlayerTeleportedEvent @event)
    {
        if (@event.NetworkObject == null)
            return;

        if (target != null && @event.NetworkObject.transform == target)
            ForceUpdatePosition();
    }

    private void TryAssignLocalPlayerTarget(string source)
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
            return;

        NetworkObject localPlayer = NetworkManager.Singleton.SpawnManager?.GetLocalPlayerObject();
        if (localPlayer == null || !localPlayer.IsOwner)
            return;

        AssignTarget(localPlayer.transform, localPlayer.GetComponent<Rigidbody>(), source);
    }

    private void AssignTarget(Transform newTarget, Rigidbody newRigidbody, string source)
    {
        if (newTarget == null)
            return;

        target = newTarget;
        targetRigidbody = newRigidbody != null ? newRigidbody : newTarget.GetComponent<Rigidbody>();
        EnsureRaceCameraPriority();
        ForceUpdatePosition();
    }

    private void EnsureRaceCameraPriority()
    {
        RaceContext raceContext = FindFirstObjectByType<RaceContext>();
        if (raceContext?.raceCamera == null)
            return;

        if (raceContext.raceCamera.Priority < RaceCameraPlayPriority)
            raceContext.raceCamera.Priority = RaceCameraPlayPriority;
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

        Vector3 targetPosition = target.position - target.forward * distance + Vector3.up * height;

        if (speed > 2f)
            targetPosition += target.forward * (speed * 0.1f);

        float turnSharpness = rb ? rb.angularVelocity.magnitude : 0f;
        float turnBoost = Mathf.Clamp01(turnSharpness / 2f);
        float smoothTime = Mathf.Lerp(0.03f, 0.1f, 1f - turnBoost);

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

        float speedFactor = Mathf.Clamp01(speed / maxSpeedForTilt);
        float currentTiltAngle = Mathf.Lerp(minTiltAngle, maxTiltAngle, speedFactor);

        Vector3 lookDir = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
        Quaternion targetRotation = Quaternion.LookRotation(lookDir.normalized);
        Quaternion tiltRotation = Quaternion.Euler(currentTiltAngle, targetRotation.eulerAngles.y, 0);

        float rotSpeed = rotationSpeed * (1f + turnBoost * 2f);
        transform.rotation = Quaternion.Slerp(transform.rotation, tiltRotation, rotSpeed * Time.deltaTime);
    }
    #endregion
}
