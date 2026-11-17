using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Follow camera for the local player's hover car during countdown and play. Drives the transform of
/// the race virtual camera it sits on.
/// </summary>
public class SimpleHoverChaseCam : MonoBehaviour
{
    private const float MinSpeedForLead = 2f;
    private const float LeadPerSpeed = 0.1f;
    private const float TurnSpeedForFullBoost = 2f;
    private const float SlowestSmoothTime = 0.1f;
    private const float FastestSmoothTime = 0.03f;

    [Header("Settings")]
    [SerializeField] private float distance = 8;
    [SerializeField] private float height = 3;
    [SerializeField] private float rotationSpeed = 5;
    [SerializeField] private float minTiltAngle = 5;
    [SerializeField] private float maxTiltAngle = 15;
    [SerializeField] private float maxSpeedForTilt = 45;

    private Vector3 velocity;
    private GameManager gameManager;
    private Transform target;
    private Rigidbody targetRigidbody;
    private EventBinding<GameStateChangeEvent> gameStateChangeEvent;
    private EventBinding<PlayerSpawnedEvent> playerSpawnedEvent;
    private EventBinding<PlayerTeleportedEvent> playerTeleportedEvent;

    private void Start()
    {
        gameManager = FindFirstObjectByType<GameManager>();

        gameStateChangeEvent = new EventBinding<GameStateChangeEvent>(HandleGameStateChange);
        EventBus<GameStateChangeEvent>.Register(gameStateChangeEvent);

        playerSpawnedEvent = new EventBinding<PlayerSpawnedEvent>(HandlePlayerSpawned);
        EventBus<PlayerSpawnedEvent>.Register(playerSpawnedEvent);

        playerTeleportedEvent = new EventBinding<PlayerTeleportedEvent>(HandlePlayerTeleported);
        EventBus<PlayerTeleportedEvent>.Register(playerTeleportedEvent);

        TryAssignLocalPlayerTarget();
    }

    private void LateUpdate()
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

    /// <summary>
    /// Snaps the camera to the target immediately; used after teleports.
    /// </summary>
    public void ForceUpdatePosition()
    {
        if (target == null)
            return;

        transform.position = GetDesiredPosition(GetTargetSpeed());
        transform.rotation = GetDesiredRotation(GetTargetSpeed());
        velocity = Vector3.zero;
    }

    private void HandleGameStateChange(GameStateChangeEvent @event)
    {
        if (@event.NewState is PlayState)
            TryAssignLocalPlayerTarget();
    }

    private void HandlePlayerSpawned(PlayerSpawnedEvent @event)
    {
        if (@event.NetworkObject == null || !@event.NetworkObject.IsOwner)
            return;

        if (@event.NetworkObject.TryGetComponent(out PlayerController playerController) && playerController.IsBot)
            return;

        AssignTarget(@event.NetworkObject.transform);
    }

    private void HandlePlayerTeleported(PlayerTeleportedEvent @event)
    {
        if (@event.NetworkObject != null && target != null && @event.NetworkObject.transform == target)
            ForceUpdatePosition();
    }

    private void TryAssignLocalPlayerTarget()
    {
        if (NetworkManager.Singleton == null || !NetworkManager.Singleton.IsConnectedClient)
            return;

        NetworkObject localPlayer = NetworkManager.Singleton.SpawnManager?.GetLocalPlayerObject();
        if (localPlayer == null || !localPlayer.IsOwner)
            return;

        AssignTarget(localPlayer.transform);
    }

    private void AssignTarget(Transform newTarget)
    {
        target = newTarget;
        targetRigidbody = newTarget.GetComponent<Rigidbody>();

        // A player joining mid-round has no state change to hand it the view.
        if (gameManager != null && gameManager.CurrentMatchPhase == RaceContext.MatchPhase.Playing)
            gameManager.Context.ActivateRaceCamera();

        ForceUpdatePosition();
    }

    private void MoveCamera()
    {
        float speed = GetTargetSpeed();
        float turnBoost = targetRigidbody != null
            ? Mathf.Clamp01(targetRigidbody.angularVelocity.magnitude / TurnSpeedForFullBoost)
            : 0f;

        float smoothTime = Mathf.Lerp(FastestSmoothTime, SlowestSmoothTime, 1f - turnBoost);
        transform.position = Vector3.SmoothDamp(transform.position, GetDesiredPosition(speed), ref velocity, smoothTime);

        float rotSpeed = rotationSpeed * (1f + turnBoost * 2f);
        transform.rotation = Quaternion.Slerp(transform.rotation, GetDesiredRotation(speed), rotSpeed * Time.deltaTime);
    }

    private float GetTargetSpeed()
    {
        return targetRigidbody != null ? targetRigidbody.linearVelocity.magnitude : 0f;
    }

    private Vector3 GetDesiredPosition(float speed)
    {
        Vector3 position = target.position - target.forward * distance + Vector3.up * height;

        if (speed > MinSpeedForLead)
            position += target.forward * (speed * LeadPerSpeed);

        return position;
    }

    private Quaternion GetDesiredRotation(float speed)
    {
        Vector3 lookDir = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
        float yaw = lookDir.sqrMagnitude > Mathf.Epsilon
            ? Quaternion.LookRotation(lookDir).eulerAngles.y
            : transform.eulerAngles.y;

        float tilt = Mathf.Lerp(minTiltAngle, maxTiltAngle, Mathf.Clamp01(speed / maxSpeedForTilt));
        return Quaternion.Euler(tilt, yaw, 0f);
    }
}
