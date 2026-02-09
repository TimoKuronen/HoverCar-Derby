using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HoverCarControl : MonoBehaviour
{
    [SerializeField] private GameObject[] hoverPoints;

    [SerializeField] private float hoverForce;
    [SerializeField] private float hoverHeight;

    [Header("Optimization Settings")]
    [SerializeField] private bool enableOptimizations = true;
    [SerializeField] private int raycastUpdateFrequency = 2; // Update every Nth FixedUpdate (1 = every frame, 2 = every other frame)
    [SerializeField] private float positionChangeThreshold = 0.1f; // Only re-raycast if car moved this much
    [SerializeField] private float maxRaycastDistance = 10f; // Early exit if car is too high

    private int layerMask;
    private Rigidbody rig;
    private bool isHovering = true;

    // Caching for optimization
    private RaycastHit[] cachedHits;
    private Vector3 lastPosition;
    private int fixedUpdateCounter = 0;
    private bool needsRaycastUpdate = true;

    void Start()
    {
        rig = GetComponent<Rigidbody>();
        layerMask = 1 << LayerMask.NameToLayer("Terrain");
        
        // Initialize cache
        cachedHits = new RaycastHit[hoverPoints.Length];
        lastPosition = transform.position;
        
        // Pre-populate cache on first frame
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
        // Only apply hover force if hovering is enabled
        if (!isHovering)
            return;

        // Optimization: Check if car is too high above ground (early exit)
        if (enableOptimizations && transform.position.y > maxRaycastDistance)
        {
            // Apply downward force if too high
            rig.AddForce(0.5f * hoverForce * Vector3.down, ForceMode.Acceleration);
            return;
        }

        // Optimization: Update raycast cache less frequently
        if (enableOptimizations)
        {
            fixedUpdateCounter++;
            
            // Check if we need to update raycasts
            bool shouldUpdate = false;
            
            if (fixedUpdateCounter >= raycastUpdateFrequency)
            {
                fixedUpdateCounter = 0;
                shouldUpdate = true;
            }
            
            // Also update if car moved significantly
            if (Vector3.Distance(transform.position, lastPosition) > positionChangeThreshold)
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
            // Non-optimized path: Update every frame
            UpdateRaycastCache();
        }

        // Apply forces using cached raycast data
        for (int i = 0; i < hoverPoints.Length; i++)
        {
            var hoverPoint = hoverPoints[i];
            
            if (enableOptimizations && i < cachedHits.Length && cachedHits[i].collider != null)
            {
                // Use cached hit data
                var hit = cachedHits[i];
                float normalizedDistance = hit.distance / hoverHeight;
                float forceMultiplier = Mathf.Clamp01(1.0f - normalizedDistance);
                rig.AddForceAtPosition(forceMultiplier * hoverForce * Vector3.up, hoverPoint.transform.position);
            }
            else
            {
                // Fallback: Direct raycast (for non-optimized path or cache miss)
                if (Physics.Raycast(hoverPoint.transform.position, -Vector3.up, out RaycastHit hit, hoverHeight, layerMask))
                {
                    float normalizedDistance = hit.distance / hoverHeight;
                    float forceMultiplier = Mathf.Clamp01(1.0f - normalizedDistance);
                    rig.AddForceAtPosition(forceMultiplier * hoverForce * Vector3.up, hoverPoint.transform.position);
                }
                else
                {
                    // Apply fallback force when no ground detected
                    if (transform.position.y > hoverPoint.transform.position.y)
                        rig.AddForceAtPosition(hoverPoint.transform.up * hoverForce, hoverPoint.transform.position);
                    else
                        rig.AddForceAtPosition(hoverPoint.transform.up * -hoverForce, hoverPoint.transform.position);
                }
            }
        }
    }

    /// <summary>
    /// Updates the raycast cache. Called less frequently than every frame for optimization.
    /// </summary>
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
