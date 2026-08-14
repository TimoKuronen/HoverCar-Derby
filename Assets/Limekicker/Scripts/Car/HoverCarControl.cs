using UnityEngine;

/// <summary>
/// Applies hover forces at raycast points to keep the car aloft.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
public class HoverCarControl : MonoBehaviour
{
    [SerializeField] private GameObject[] hoverPoints;

    [SerializeField] private float hoverForce;
    [SerializeField] private float hoverHeight;

    [Header("Optimization Settings")]
    [SerializeField] private bool enableOptimizations = true;
    [SerializeField] private int raycastUpdateFrequency = 2;
    [SerializeField] private float positionChangeThreshold = 0.1f;
    [SerializeField] private float maxRaycastDistance = 10f;

    private int layerMask;
    private Rigidbody rig;
    private bool isHovering = true;

    private RaycastHit[] cachedHits;
    private Vector3 lastPosition;
    private int fixedUpdateCounter = 0;
    private float positionChangeThresholdSq;

    void Start()
    {
        rig = GetComponent<Rigidbody>();
        layerMask = 1 << LayerMask.NameToLayer("Default");
        positionChangeThresholdSq = positionChangeThreshold * positionChangeThreshold;
        cachedHits = new RaycastHit[hoverPoints.Length];
        lastPosition = transform.position;
        UpdateRaycastCache();
    }

    public void ToggleHovering(bool value)
    {
        isHovering = value;
    }

    void FixedUpdate()
    {
        ApplyHoverForce();
    }

    private void ApplyHoverForce()
    {
        if (!isHovering)
            return;

        if (enableOptimizations && transform.position.y > maxRaycastDistance)
        {
            rig.AddForce(0.5f * hoverForce * Vector3.down, ForceMode.Acceleration);
            return;
        }

        if (enableOptimizations)
        {
            fixedUpdateCounter++;

            bool shouldUpdate = false;

            if (fixedUpdateCounter >= raycastUpdateFrequency)
            {
                fixedUpdateCounter = 0;
                shouldUpdate = true;
            }

            if ((transform.position - lastPosition).sqrMagnitude > positionChangeThresholdSq)
            {
                shouldUpdate = true;
                lastPosition = transform.position;
            }

            if (shouldUpdate)
            {
                UpdateRaycastCache();
            }
        }
        else
        {
            UpdateRaycastCache();
        }

        for (int i = 0; i < hoverPoints.Length; i++)
        {
            var hoverPoint = hoverPoints[i];
            
            if (enableOptimizations && i < cachedHits.Length && cachedHits[i].collider != null)
            {
                var hit = cachedHits[i];
                float normalizedDistance = hit.distance / hoverHeight;
                float forceMultiplier = Mathf.Clamp01(1.0f - normalizedDistance);
                rig.AddForceAtPosition(forceMultiplier * hoverForce * Vector3.up, hoverPoint.transform.position);
            }
            else
            {
                if (Physics.Raycast(hoverPoint.transform.position, -Vector3.up, out RaycastHit hit, hoverHeight, layerMask))
                {
                    float normalizedDistance = hit.distance / hoverHeight;
                    float forceMultiplier = Mathf.Clamp01(1.0f - normalizedDistance);
                    rig.AddForceAtPosition(forceMultiplier * hoverForce * Vector3.up, hoverPoint.transform.position);
                }
                else
                {
                    if (transform.position.y > hoverPoint.transform.position.y)
                        rig.AddForceAtPosition(hoverPoint.transform.up * hoverForce, hoverPoint.transform.position);
                    else
                        rig.AddForceAtPosition(hoverPoint.transform.up * -hoverForce, hoverPoint.transform.position);
                }
            }
        }
    }

    private void UpdateRaycastCache()
    {
        if (cachedHits == null || cachedHits.Length != hoverPoints.Length)
        {
            cachedHits = new RaycastHit[hoverPoints.Length];
        }

        for (int i = 0; i < hoverPoints.Length; i++)
        {
            var hoverPoint = hoverPoints[i];
            Physics.Raycast(hoverPoint.transform.position, -Vector3.up, out cachedHits[i], hoverHeight, layerMask);
        }
    }

    void OnDrawGizmos()
    {
        if (hoverPoints == null) return;

        for (int i = 0; i < hoverPoints.Length; i++)
        {
            var hoverPoint = hoverPoints[i];
            if (hoverPoint == null) continue;

            int terrainLayer = LayerMask.NameToLayer("Terrain");
            int mask = terrainLayer >= 0 ? 1 << terrainLayer : -1;
            
            if (Physics.Raycast(hoverPoint.transform.position, -Vector3.up, out RaycastHit hit, hoverHeight, mask))
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
