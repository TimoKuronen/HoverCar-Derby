using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HoverCarControl : MonoBehaviour
{
    [SerializeField] private GameObject[] hoverPoints;

    [SerializeField] private float hoverForce;
    [SerializeField] private float hoverHeight;

    private int layerMask;
    private Rigidbody rig;
    private bool isHovering = true;

    void Start()
    {
        rig = GetComponent<Rigidbody>();
        layerMask = 1 << LayerMask.NameToLayer("Terrain");
    }

    public void ToggleHovering(bool value)
    {
        isHovering = value;
        if (value == true)
            Debug.Break();
    }

    void FixedUpdate()
    {
        ApplyHoverForce();
    }

    private void ApplyHoverForce()
    {
        // Only apply hover force if hovering is enabled
        if (!isHovering)
            return;

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