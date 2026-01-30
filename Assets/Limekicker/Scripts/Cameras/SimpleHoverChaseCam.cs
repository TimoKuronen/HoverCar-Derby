using System;
using Unity.Netcode;
using UnityEngine;

public class SimpleHoverChaseCam : MonoBehaviour
{
    [Header("Settings")]
    public float distance = 8;
    public float height = 3;
    public float rotationSpeed = 5;
    public float minTiltAngle = 5, maxTiltAngle = 15, maxSpeedForTilt = 45;
    public Vector3 velocity;

    private Transform target;
    private Rigidbody targetRigidbody;

    public int TryAssignLocalPlayer { get; private set; }

    private EventBinding<GameStateChangeEvent> gameStateChangeEvent;
    private EventBinding<PlayerSpawnedEvent> playerSpawnedEvent;

    public void Start()
    {
        gameStateChangeEvent = new EventBinding<GameStateChangeEvent>(HandleGameStateChange);
        EventBus<GameStateChangeEvent>.Register(gameStateChangeEvent);

        playerSpawnedEvent = new EventBinding<PlayerSpawnedEvent>(HandlePlayerSpawnFromManager);
        EventBus<PlayerSpawnedEvent>.Register(playerSpawnedEvent);
    }

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
        if (playerSpawnedEvent.NetworkObject != null && playerSpawnedEvent.NetworkObject.IsOwner)
        {
            AssignTarget(playerSpawnedEvent.NetworkObject.transform, playerSpawnedEvent.NetworkObject.GetComponent<Rigidbody>());
            //Debug.Log($"[SimpleHoverChaseCam] Assigned target from PlayerSpawnManager: {netObj.name}");
        }
    }

    private void AssignTarget(Transform newTarget, Rigidbody newRigidbody)
    {
        if (newTarget == null)
            return;

        target = newTarget;
        targetRigidbody = newRigidbody != null ? newRigidbody : newTarget.GetComponent<Rigidbody>();
    }

    void LateUpdate()
    {
        MoveCamera();
    }

    private void MoveCamera()
    {
        if (!target)
            return;

        Rigidbody rb = target.GetComponent<Rigidbody>();
        float speed = rb.velocity.magnitude;

        // Define camera target position (behind & above the car)
        Vector3 targetPosition = target.position - target.forward * distance + Vector3.up * height;

        // Apply look-ahead effect if moving fast
        if (speed > 2f)
        {
            targetPosition += target.forward * (speed * 0.1f);
        }

        float turnSharpness = rb ? rb.angularVelocity.magnitude : 0f;
        float turnBoost = Mathf.Clamp01(turnSharpness / 2f);
        float smoothTime = Mathf.Lerp(0.03f, 0.1f, 1f - turnBoost);

        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, smoothTime);

        float speedFactor = Mathf.Clamp01(speed / maxSpeedForTilt);
        float currentTiltAngle = Mathf.Lerp(minTiltAngle, maxTiltAngle, speedFactor);

        Vector3 lookDir = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
        Quaternion targetRotation = Quaternion.LookRotation(lookDir.normalized);
        Quaternion tiltRotation = Quaternion.Euler(currentTiltAngle, targetRotation.eulerAngles.y, 0);

        // Faster rotation follow
        float rotSpeed = rotationSpeed * (1f + turnBoost * 2f);  // boost when turning
        transform.rotation = Quaternion.Slerp(transform.rotation, tiltRotation, rotSpeed * Time.deltaTime);
    }

    private void OnDestroy()
    {
        EventBus<GameStateChangeEvent>.Unregister(gameStateChangeEvent);
        EventBus<PlayerSpawnedEvent>.Unregister(playerSpawnedEvent);
    }
}
