using System;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using VContainer;

[RequireComponent(typeof(Camera))]
public class SimpleHoverChaseCam : MonoBehaviour
{
    [Header("Offsets & Settings")]
    public Vector3 localOffset = new Vector3(0f, 3.5f, -7f);
    [Range(0f, 40f)] public float fixedPitchDegrees = 12f;
    public float positionSmoothTime = 0.15f;
    public float yawLerpSpeed = 8f;

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
        //Version1();
        Version2();
    }

    private void Version1()
    {
        if (target == null)
        {
            return;
        }

        Vector3 heading;
        if (targetRigidbody && targetRigidbody.velocity.sqrMagnitude > 0.01f)
        {
            heading = Vector3.ProjectOnPlane(targetRigidbody.velocity, Vector3.up).normalized;
            if (heading.sqrMagnitude < 0.0001f)
                heading = Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized;
        }
        else
        {
            heading = Vector3.ProjectOnPlane(target.forward, Vector3.up).normalized;
        }

        float targetYawDeg = Mathf.Atan2(heading.x, heading.z) * Mathf.Rad2Deg;

        if (!initialized)
        {
            currentYawDeg = targetYawDeg;
            initialized = true;
        }
        else
        {
            currentYawDeg = Mathf.LerpAngle(currentYawDeg, targetYawDeg, Time.deltaTime * yawLerpSpeed);
        }

        Quaternion yawOnly = Quaternion.Euler(0f, currentYawDeg, 0f);
        Vector3 desiredPos = target.position + yawOnly * localOffset;
        transform.position = Vector3.SmoothDamp(transform.position, desiredPos, ref posVel, positionSmoothTime);
        transform.rotation = Quaternion.Euler(fixedPitchDegrees, currentYawDeg, 0f);
    }

    void Version2()
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
            targetPosition += target.forward * (speed * lookAheadMultiplier * 0.1f);
        }

        // Smoothly move the camera to the target position
        transform.position = Vector3.SmoothDamp(transform.position, targetPosition, ref velocity, 0.1f);

        // Calculate dynamic tilt angle based on speed
        float speedFactor = Mathf.Clamp01(speed / maxSpeedForTilt);
        float currentTiltAngle = Mathf.Lerp(minTiltAngle, maxTiltAngle, speedFactor);

        // Create rotation that looks at the car but adjusts tilt
        Quaternion targetRotation = Quaternion.LookRotation(target.position - transform.position);
        Quaternion tiltRotation = Quaternion.Euler(currentTiltAngle, targetRotation.eulerAngles.y, 0);

        // Smoothly rotate the camera towards this new rotation
        transform.rotation = Quaternion.Slerp(transform.rotation, tiltRotation, rotationSpeed * Time.deltaTime);
    }
}
