using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HoverCarControl_backup : MonoBehaviour
{
    [SerializeField] private GameObject[] hoverPoints;
    [SerializeField] private GameObject leftAirBrake;
    [SerializeField] private GameObject rightAirBrake;

    [SerializeField] private float inputDeadZone = 0.1f;
    [SerializeField] private float hoverForce;
    [SerializeField] private float hoverHeight;
    [SerializeField] private float forwardAcceleration;
    [SerializeField] private float backwardAcceleration;
    [SerializeField] private float currentThrust = 0.0f;
    [SerializeField] private float turnStrength = 10f;

    private int layerMask;
    private float backForce;
    private float accelerationAxis;
    private float maxSpeed;
    private float origDrag;
    private float currentTurn;
    private float origForwardValue;

    private Vector3 direction;
    private Vector3 pos;
    private Vector3 directionVector;
    private Vector3 targetDir;
    private Quaternion new_rotation;

    private Rigidbody rig;

    [Space, Header("For debugging purposes")]
    [SerializeField] private float turnTorque;
    [SerializeField] private float forwardForce;

    void Start()
    {
        SetDefaultValues();
    }

    private void SetDefaultValues()
    {
        rig = GetComponent<Rigidbody>();
        origForwardValue = forwardAcceleration;
        layerMask = 1 << LayerMask.NameToLayer("Players");
        layerMask = ~layerMask;
        rig.maxAngularVelocity = 60;
        maxSpeed = 10;
        origDrag = rig.drag;
    }

    void Update()
    {
        GetInput();
        GetMovementVelocity();
        GetTurningData();
    }

    private void GetInput()
    {
        accelerationAxis = Input.GetKeyDown(KeyCode.W) ? 0 : 1;
        backForce = Input.GetKeyDown(KeyCode.S) ? 0 : 1;
        direction = new Vector3(Input.GetAxis("Vertical"), 0, -Input.GetAxis("Horizontal"));
    }

    private void GetTurningData()
    {
        currentThrust = Mathf.Max(Mathf.Abs(direction.x), Mathf.Abs(direction.z)) * forwardAcceleration;

        pos = new Vector3(Input.mousePosition.x, transform.position.y, Input.mousePosition.z);
        directionVector = pos - transform.position;

        targetDir.Set(directionVector.x, 0.0f, directionVector.z);
        new_rotation = Quaternion.LookRotation(targetDir);
        transform.rotation = Quaternion.Slerp(transform.rotation, new_rotation, Time.deltaTime * 5);
    }

    private void GetMovementVelocity()
    {
        if (currentThrust == 0)
            rig.drag = origDrag * 2;
        else rig.drag = origDrag;

        // Main Thrust
        currentThrust = 0.0f;
        if (accelerationAxis > inputDeadZone)
        {
            currentThrust = accelerationAxis * forwardAcceleration - backForce * backwardAcceleration;
        }
        else if (backForce > inputDeadZone)
            currentThrust = -backForce * backwardAcceleration;
    }

    void FixedUpdate()
    {
        ApplyHoverForce();
        ApplyMovement();
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
        if (Mathf.Abs(currentThrust) > 0)
            rig.AddForce(direction * currentThrust);

        if (currentTurn > 0)
        {
            rig.AddRelativeTorque(Vector3.up * currentTurn * turnStrength);
        }
        else if (currentTurn < 0)
        {
            rig.AddRelativeTorque(Vector3.up * currentTurn * turnStrength);
        }

        if (rig.velocity.magnitude > maxSpeed)
        {
            rig.velocity = rig.velocity.normalized * maxSpeed;
        }

        turnTorque = rig.angularVelocity.magnitude;
        forwardForce = rig.velocity.magnitude;
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