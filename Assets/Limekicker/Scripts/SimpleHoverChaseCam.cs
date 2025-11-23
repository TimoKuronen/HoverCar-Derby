using Unity.Netcode;
using UnityEngine;
using VContainer;

[RequireComponent(typeof(Camera))]
public class SimpleHoverChaseCam : MonoBehaviour
{
    [Header("Settings")]
    public float distance = 8;
    public float height = 3;
    public float rotationSpeed = 5;
    public float minTiltAngle = 5, maxTiltAngle = 15, maxSpeedForTilt = 45;
    public Vector3 velocity;

    private Vector3 posVel;
    private Transform target;
    private Rigidbody targetRigidbody;

    public int TryAssignLocalPlayer { get; private set; }

    [Inject]
    public void Construct(IPlayerSpawnManager spawnManager)
    {
        spawnManager.OnPlayerSpawned += OnPlayerSpawnedFromManager;
        
        // Also listen to PlayerController static event which fires on all clients
        PlayerController.OnPlayerSpawned += OnPlayerControllerSpawned;
        
        // Check if local player already exists (in case we registered after spawn)
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.IsConnectedClient)
        {
            var localPlayer = NetworkManager.Singleton.SpawnManager?.GetLocalPlayerObject();
            if (localPlayer != null)
            {
                OnPlayerSpawnedFromManager(null, localPlayer);
            }
            
            // Also check via PlayerController
            var playerController = localPlayer?.GetComponent<PlayerController>();
            if (playerController != null && playerController.IsOwner)
            {
                OnPlayerControllerSpawned(playerController);
            }
        }
    }

    private void OnPlayerSpawnedFromManager(UserData data, NetworkObject netObj)
    {
        if (netObj != null && netObj.IsOwner)
        {
            AssignTarget(netObj.transform, netObj.GetComponent<Rigidbody>());
            //Debug.Log($"[SimpleHoverChaseCam] Assigned target from PlayerSpawnManager: {netObj.name}");
        }
    }

    private void OnPlayerControllerSpawned(PlayerController controller)
    {
        if (controller != null && controller.IsOwner)
        {
            AssignTarget(controller.transform, controller.GetComponent<Rigidbody>());
            //Debug.Log($"[SimpleHoverChaseCam] Assigned target from PlayerController event: {controller.name}");
        }
    }

    private void AssignTarget(Transform newTarget, Rigidbody newRigidbody)
    {
        if (newTarget == null) 
            return;
        
        target = newTarget;
        targetRigidbody = newRigidbody != null ? newRigidbody : newTarget.GetComponent<Rigidbody>();
    }

    private void OnDestroy()
    {
        PlayerController.OnPlayerSpawned -= OnPlayerControllerSpawned;
    }

    void LateUpdate()
    {
        MoveCamera();
    }

    void MoveCamera()
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
}
