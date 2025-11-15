using System;
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
    private float currentYawDeg;
    private bool initialized;
    private Transform target;
    private Rigidbody targetRigidbody;

    public int TryAssignLocalPlayer { get; private set; }

    [Inject]
    public void Construct(IPlayerSpawnManager spawnManager)
    {
        spawnManager.OnPlayerSpawned += OnPlayerSpawned;
    }

    private void OnPlayerSpawned(UserData data, NetworkObject netObj)
    {
        if (netObj.IsOwner)
        {
            target = netObj.transform;
            targetRigidbody = netObj.GetComponent<Rigidbody>();
        }

    }

    void LateUpdate()
    {
        MoveCamera();
    }

    void MoveCamera()
    {
        if (!target)
            return;

        // Get car velocity
        Rigidbody rb = target.GetComponent<Rigidbody>();
        float speed = rb.velocity.magnitude;

        // Define camera target position (behind & above the car)
        Vector3 targetPosition = target.position - target.forward * distance + Vector3.up * height;

        // Apply look-ahead effect if moving fast
        if (speed > 2f)
        {
            targetPosition += target.forward * (speed * 0.1f);
        }

        // Smoothly move the camera to the target position
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, 0.1f);

        // Calculate dynamic tilt angle based on speed
        float speedFactor = Mathf.Clamp01(speed / maxSpeedForTilt);
        float currentTiltAngle = Mathf.Lerp(minTiltAngle, maxTiltAngle, speedFactor);

        // Create rotation that looks at the car but adjusts tilt
        Vector3 lookDir = Vector3.ProjectOnPlane(target.position - transform.position, Vector3.up);
        Quaternion targetRotation = Quaternion.LookRotation(lookDir.normalized);
        Quaternion tiltRotation = Quaternion.Euler(currentTiltAngle, targetRotation.eulerAngles.y, 0);

        // Smoothly rotate the camera towards this new rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, tiltRotation, rotationSpeed * Time.deltaTime);
    }

    /// Old version
    //private void Version1()
    //{
    //    if (target == null)
    //    {
    //        return;
    //    }

    //    Vector3 heading;
    //    if (targetRigidbody && targetRigidbody.velocity.sqrMagnitude > 0.01f)
    //    {
    //        heading = Vector3.ProjectOnPlane(targetRigidbody.velocity, Vector3.up).normalized;
    //        if (heading.sqrMagnitude < 0.0001f)
    //            heading = Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized;
    //    }
    //    else
    //    {
    //        heading = Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized;
    //    }

    //    float targetYawDeg = Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg;

    //    if (!initialized)
    //    {
    //        currentYawDeg = targetYawDeg;
    //        initialized = true;
    //    }
    //    else
    //    {
    //        currentYawDeg = Mathf.LerpAngle(currentYawDeg, targetYawDeg, Time.deltaTime * yawLerpSpeed);
    //    }

    //    Quaternion yawOnly = Quaternion.Euler(0f, currentYawDeg, 0f);
    //    Vector3 desiredPos = target.position + yawOnly * localOffset;
    //    transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref posVel, positionSmoothTime);
    //    transform.rotation = Quaternion.Euler(fixedPitchDegrees, currentYawDeg, 0f);
    //}
}
