using System.Collections;
using Unity.Netcode;
using UnityEngine;

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

    public void Construct(IInputService inputService)
    {
        Debug.Log("[HoverCarMover] Constructed");
        this.inputService = inputService;
    }

    public override void OnNetworkSpawn()
    {
        if (!IsOwner)
            enabled = false;
    }

    private IEnumerator Start()
    {
        originalAccelerationValue = forwardAcceleration;
        originalMaxSpeed = maxSpeed;

        yield return new WaitUntil(() => GameSignals.IsSessionLoaded);

        rig.isKinematic = false;
        isReady = true;
    }

    private void FixedUpdate()
    {
        if (!IsOwner || !isReady)
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
}
