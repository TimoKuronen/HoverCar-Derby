using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // The car
    public float distance = 15f; // Distance from the car
    public float height = 5f; // Height above the car
    public float rotationSpeed = 3f; // How fast camera rotates towards car’s forward direction
    public float lookAheadMultiplier = 2f; // How far ahead the camera looks based on velocity
    public float minTiltAngle = 30f; // Angle when stationary
    public float maxTiltAngle = 10f; // Angle when at max speed
    public float maxSpeedForTilt = 50f; // Speed at which we reach max tilt

    private Vector3 velocity = Vector3.zero;

    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += (id) =>
        {
            if (NetworkManager.Singleton.IsHost)
            {
                target = GameObject.FindGameObjectWithTag("Vehicle").transform;
            }
        };
    }

    void LateUpdate()
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
