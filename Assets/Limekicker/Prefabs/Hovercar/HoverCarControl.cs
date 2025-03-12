using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HoverCarControl : MonoBehaviour
{
    [SerializeField] private GameObject[] hoverPoints;

    [SerializeField] private float inputDeadZone = 0.1f;
    [SerializeField] private float hoverForce;
    [SerializeField] private float hoverHeight;
    [SerializeField] private float forwardAcceleration;
    [SerializeField] private float backwardAcceleration;
    [SerializeField] private float currentThrust = 0.0f;
    [SerializeField] private float turnStrength = 10f;
    [SerializeField] private float maxSpeed;
    [SerializeField] private float maxAngularVelocity = 60f;

    private int layerMask;
    private float currentTurn;
    private Rigidbody rig;

    void Start()
    {
        rig = GetComponent<Rigidbody>();
        layerMask = 1 << LayerMask.NameToLayer("Terrain");
    }

    void Update()
    {
        GetInput();
    }

    void FixedUpdate()
    {
        rig.maxAngularVelocity = maxAngularVelocity;

        ApplyHoverForce();
        ApplyMovement();
    }

    private void GetInput()
    {
        float moveInput = Input.GetAxis("Vertical");
        float turnInput = Input.GetAxis("Horizontal");

        currentThrust = Mathf.Abs(moveInput) > inputDeadZone ? moveInput * (moveInput > 0 ? forwardAcceleration : backwardAcceleration) : 0f;
        currentTurn = Mathf.Abs(turnInput) > inputDeadZone ? turnInput * turnStrength : 0f;
    }

    private void ApplyHoverForce()
    {
        for (int i = 0; i < hoverPoints.Length; i++)
        {
            var hoverPoint = hoverPoints[i];
            if (Physics.Raycast(hoverPoint.transform.position, -Vector3.up, out RaycastHit hit, hoverHeight, layerMask))
                rig.AddForceAtPosition((1.0f - (hit.distance / hoverHeight)) * hoverForce * Vector3.up, hoverPoint.transform.position);
            else
            {
                if (transform.position.y > hoverPoint.transform.position.y)
                    rig.AddForceAtPosition(hoverPoint.transform.up * hoverForce, hoverPoint.transform.position);
                else
                    rig.AddForceAtPosition(hoverPoint.transform.up * -hoverForce, hoverPoint.transform.position);
            }
        }
    }

    private void ApplyMovement()
    {
        if (currentThrust != 0)
        {
            rig.AddForce(transform.forward * currentThrust, ForceMode.Acceleration);
        }

        if (currentTurn != 0)
        {
            rig.AddTorque(Vector3.up * currentTurn, ForceMode.Acceleration);
        }

        if (rig.velocity.magnitude > maxSpeed)
        {
            rig.velocity = rig.velocity.normalized * maxSpeed;
        }
    }

    void OnDrawGizmos()
    {
        for (int i = 0; i < hoverPoints.Length; i++)
        {
            var hoverPoint = hoverPoints[i];
            if (Physics.Raycast(hoverPoint.transform.position, -Vector3.up, out RaycastHit hit, hoverHeight, layerMask))
            {
                Gizmos.color = Color.blue;
                Gizmos.DrawLine(hoverPoint.transform.position, hit.point);
                Gizmos.DrawSphere(hit.point, 0.5f);
            }
            else
            {
                Gizmos.color = Color.red;
                Gizmos.DrawLine(hoverPoint.transform.position, hoverPoint.transform.position - Vector3.up * hoverHeight);
            }
        }
    }
}