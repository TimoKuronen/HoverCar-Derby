using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(PlayerController))]
public class ServerPhysicsCollisionHandler : NetworkBehaviour
{
    #region Fields
    [Header("Impact Settings")]
    [SerializeField] private float minImpactForce = 5f;
    [SerializeField] private float explosiveForceMultiplier = 1f;
    [SerializeField] private float upwardModifier = 0.5f;

    [Header("Damage Settings")]
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private float minDamageThreshold = 5f;
    
    [Header("Velocity Thresholds")]
    [SerializeField] private float idleSpeedThreshold = 2f; // Speed below which car is considered idle
    [SerializeField] private float decentSpeedThreshold = 8f; // Speed above which car is considered to have decent velocity

    [Header("Collision Cooldown")]
    [SerializeField] private float collisionCooldown = 0.5f;

    private Rigidbody rb;
    private PlayerController playerController;
    private CarDamageManager damageManager;
    private float lastCollisionTime;
    private static HashSet<ulong> globalRecentCollisions = new HashSet<ulong>();
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();
        damageManager = playerController.DamageManager;
    }

    /// <summary>
    /// Handles car-to-car collisions on the server. Uses a global collision tracking system
    /// to prevent duplicate damage from the same collision event.
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        if (Time.time - lastCollisionTime < collisionCooldown)
            return;

        PlayerController otherPlayer = collision.gameObject.GetComponent<PlayerController>();
        if (otherPlayer == null)
            return;

        if (IsServer)
        {
            HandleServerCollisionEnter(collision, otherPlayer);
        }
        else if (IsOwner)
        {
            HandleOwnerCollisionEnter(collision, otherPlayer);
        }
    }

    private void HandleServerCollisionEnter(Collision collision, PlayerController otherPlayer)
    {
        GetCollisionIds(otherPlayer, out ulong thisId, out ulong otherId, out ulong collisionKey);

        if (globalRecentCollisions.Contains(collisionKey))
            return;

        if (thisId > otherId)
            return;

        float impactForce = collision.relativeVelocity.magnitude;
        if (!TryRegisterCollision(collisionKey, impactForce))
            return;

        Rigidbody otherRb = otherPlayer.GetComponent<Rigidbody>();
        ProcessCarCollisionInternal(
            otherPlayer,
            collision.contacts[0].point,
            (otherPlayer.transform.position - transform.position).normalized,
            impactForce,
            collision.contacts[0].normal,
            rb.velocity.magnitude,
            otherRb != null ? otherRb.velocity.magnitude : 0f);

        lastCollisionTime = Time.time;
        StartCoroutine(RemoveFromRecentCollisions(collisionKey, collisionCooldown));
    }

    private void HandleOwnerCollisionEnter(Collision collision, PlayerController otherPlayer)
    {
        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce < minImpactForce)
            return;

        Rigidbody otherRb = otherPlayer.GetComponent<Rigidbody>();
        if (otherRb == null)
            return;

        ReportCollisionServerRpc(
            otherPlayer.NetworkObjectId,
            impactForce,
            collision.contacts[0].point,
            (otherPlayer.transform.position - transform.position).normalized,
            collision.contacts[0].normal,
            rb.velocity.magnitude,
            otherRb.velocity.magnitude);

        lastCollisionTime = Time.time;
    }

    [ServerRpc]
    private void ReportCollisionServerRpc(
        ulong otherNetworkObjectId,
        float impactForce,
        Vector3 contactPoint,
        Vector3 direction,
        Vector3 hitNormal,
        float reporterSpeed,
        float reportedOtherSpeed)
    {
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(otherNetworkObjectId, out NetworkObject otherNetObj))
            return;

        PlayerController otherPlayer = otherNetObj.GetComponent<PlayerController>();
        if (otherPlayer == null)
            return;

        GetCollisionIds(otherPlayer, out _, out _, out ulong collisionKey);

        if (globalRecentCollisions.Contains(collisionKey))
            return;

        if (!TryRegisterCollision(collisionKey, impactForce))
            return;

        ProcessCarCollisionInternal(
            otherPlayer,
            contactPoint,
            direction,
            impactForce,
            hitNormal,
            reporterSpeed,
            reportedOtherSpeed);

        lastCollisionTime = Time.time;
        StartCoroutine(RemoveFromRecentCollisions(collisionKey, collisionCooldown));
    }
    #endregion

    #region Private Methods
    private IEnumerator RemoveFromRecentCollisions(ulong collisionKey, float delay)
    {
        yield return new WaitForSeconds(delay);
        globalRecentCollisions.Remove(collisionKey);
    }

    private static void GetCollisionIds(PlayerController otherPlayer, PlayerController thisPlayer, NetworkBehaviour thisNetworkBehaviour, out ulong thisId, out ulong otherId, out ulong collisionKey)
    {
        bool thisIsBot = thisPlayer.IsBot;
        bool otherIsBot = otherPlayer.IsBot;

        thisId = thisIsBot ? thisNetworkBehaviour.NetworkObjectId : thisNetworkBehaviour.OwnerClientId;
        otherId = otherIsBot ? otherPlayer.NetworkObjectId : otherPlayer.OwnerClientId;
        collisionKey = thisId < otherId ? (thisId << 32) | otherId : (otherId << 32) | thisId;
    }

    private void GetCollisionIds(PlayerController otherPlayer, out ulong thisId, out ulong otherId, out ulong collisionKey)
    {
        GetCollisionIds(otherPlayer, playerController, this, out thisId, out otherId, out collisionKey);
    }

    private bool TryRegisterCollision(ulong collisionKey, float impactForce)
    {
        if (impactForce < minImpactForce)
            return false;

        globalRecentCollisions.Add(collisionKey);
        return true;
    }

    /// <summary>
    /// Processes collision damage and physics forces based on collision type, velocities, and impact angles.
    /// Damage distribution follows rules: front bumper hits deal full damage, head-on collisions split damage,
    /// and side hits require majority velocity to deal damage.
    /// </summary>
    private void ProcessCarCollisionInternal(
        PlayerController otherPlayer,
        Vector3 contactPoint,
        Vector3 direction,
        float impactForce,
        Vector3 hitNormal,
        float thisSpeed,
        float otherSpeed)
    {
        // Determine collision angles - front bumper hits are more damaging
        bool thisIsFrontBumper = Vector3.Dot(transform.forward, -hitNormal) > 0.7f;
        bool otherIsFrontBumper = Vector3.Dot(otherPlayer.transform.forward, hitNormal) > 0.7f;
        // Check if hitting with side (not front bumper, and hit normal aligns with side direction)
        // Using absolute value since we're checking alignment with either left or right side
        bool thisIsSide = !thisIsFrontBumper && Mathf.Abs(Vector3.Dot(transform.right, -hitNormal)) > 0.5f;
        bool otherIsSide = !otherIsFrontBumper && Mathf.Abs(Vector3.Dot(otherPlayer.transform.right, hitNormal)) > 0.5f;

        bool isHeadOn = thisIsFrontBumper && otherIsFrontBumper;
        float baseDamage = impactForce * damageMultiplier;

        bool thisIsIdle = thisSpeed < idleSpeedThreshold;
        bool otherIsIdle = otherSpeed < idleSpeedThreshold;
        bool thisHasDecentSpeed = thisSpeed >= decentSpeedThreshold;
        bool otherHasDecentSpeed = otherSpeed >= decentSpeedThreshold;

        float damageToThis = 0f;
        float damageToOther = 0f;

        if (isHeadOn)
        {
            // Front-to-front collision: split based on velocity difference
            float totalDamage = baseDamage * 0.5f;
            float speedDifference = Mathf.Abs(thisSpeed - otherSpeed);
            float totalSpeed = thisSpeed + otherSpeed;
            
            if (totalSpeed < 0.01f)
            {
                // Both essentially idle - minimal damage split evenly
                damageToThis = totalDamage * 0.5f;
                damageToOther = totalDamage * 0.5f;
            }
            else
            {
                // Split damage based on velocity difference
                float thisRatio = thisSpeed / totalSpeed;
                float otherRatio = otherSpeed / totalSpeed;
                
                damageToThis = totalDamage * otherRatio;
                damageToOther = totalDamage * thisRatio;
            }
        }
        else if (thisIsFrontBumper)
        {
            // This car hits with front bumper
            if (thisIsIdle)
            {
                // Idle car shouldn't deal much damage even with front bumper
                damageToThis = 0f;
                damageToOther = baseDamage * 0.1f; // Minimal damage from idle car
            }
            else if (otherIsIdle || !otherHasDecentSpeed)
            {
                // This car has velocity, other is idle/slow - this car deals damage
                float velocityMultiplier = Mathf.Clamp01(thisSpeed / decentSpeedThreshold);
                damageToThis = 0f;
                damageToOther = baseDamage * velocityMultiplier;
            }
            else
            {
                // Both have decent velocity - front bumper deals all damage
                damageToThis = 0f;
                damageToOther = baseDamage;
            }
        }
        else if (otherIsFrontBumper)
        {
            // Other car hits with front bumper
            if (otherIsIdle)
            {
                // Idle car shouldn't deal much damage even with front bumper
                damageToThis = baseDamage * 0.1f; // Minimal damage from idle car
                damageToOther = 0f;
            }
            else if (thisIsIdle || !thisHasDecentSpeed)
            {
                // Other car has velocity, this is idle/slow - other car deals damage
                float velocityMultiplier = Mathf.Clamp01(otherSpeed / decentSpeedThreshold);
                damageToThis = baseDamage * velocityMultiplier;
                damageToOther = 0f;
            }
            else
            {
                // Both have decent velocity - front bumper deals all damage
                damageToThis = baseDamage;
                damageToOther = 0f;
            }
        }
        else
        {
            // Sideways collision - check for majority velocity
            // A car with majority velocity hitting with its side should deal damage if the other is idle/slow
            bool thisHasMajorityVelocity = thisSpeed > otherSpeed * 1.5f; // This car has significantly more velocity
            bool otherHasMajorityVelocity = otherSpeed > thisSpeed * 1.5f; // Other car has significantly more velocity
            
            if (thisHasMajorityVelocity && thisIsSide && (otherIsIdle || !otherHasDecentSpeed))
            {
                // This car has majority velocity, hits with side, other is idle/slow - this car gets points
                float velocityMultiplier = Mathf.Clamp01((thisSpeed - otherSpeed) / decentSpeedThreshold);
                damageToThis = 0f;
                damageToOther = baseDamage * velocityMultiplier * 0.7f; // Side hits deal less damage
            }
            else if (otherHasMajorityVelocity && otherIsSide && (thisIsIdle || !thisHasDecentSpeed))
            {
                // Other car has majority velocity, hits with side, this is idle/slow - other car gets points
                float velocityMultiplier = Mathf.Clamp01((otherSpeed - thisSpeed) / decentSpeedThreshold);
                damageToThis = baseDamage * velocityMultiplier * 0.7f; // Side hits deal less damage
                damageToOther = 0f;
            }
            else if (thisIsIdle && otherIsIdle)
            {
                // Both idle - minimal damage split evenly
                damageToThis = baseDamage * 0.1f;
                damageToOther = baseDamage * 0.1f;
            }
            else
            {
                // Neither has clear majority or both moving - split damage
                damageToThis = baseDamage * 0.5f;
                damageToOther = baseDamage * 0.5f;
            }
        }

        if (damageToThis > 0)
            ApplyDamageToCar(otherPlayer, contactPoint, damageToThis);

        if (damageToOther > minDamageThreshold)
            ApplyDamageToOtherCar(otherPlayer, contactPoint, damageToOther);

        ApplyPhysicsForces(otherPlayer, contactPoint, direction, impactForce);
    }

    private void ApplyDamageToCar(PlayerController otherPlayer, Vector3 contactPoint, float damage)
    {
        if (damageManager == null)
            return;

        ulong? attackerId = otherPlayer.IsBot ? otherPlayer.NetworkObjectId : otherPlayer.OwnerClientId;
        damageManager.ApplyDamage(damage, contactPoint, attackerId);
        Debug.Log($"[ServerPhysicsCollisionHandler] Applied {damage} damage to {playerController.PlayerName.Value} from collision.");
    }

    private void ApplyDamageToOtherCar(PlayerController otherPlayer, Vector3 contactPoint, float damage)
    {
        CarDamageManager otherDM = otherPlayer.DamageManager;
        if (otherDM != null)
        {
            ulong attackerId = playerController.IsBot ? NetworkObjectId : OwnerClientId;
            otherDM.ApplyDamage(damage, contactPoint, attackerId);
            Debug.Log($"[ServerPhysicsCollisionHandler] Applied {damage} damage to {otherPlayer.PlayerName.Value} from collision with {playerController.PlayerName.Value}.");
        }
    }

    /// <summary>
    /// Applies physics forces to both cars involved in the collision. The car that was hit takes less force
    /// than the car doing the hitting. Also applies upward force for a "pop" effect.
    /// </summary>
    private void ApplyPhysicsForces(PlayerController otherPlayer, Vector3 collisionPoint, Vector3 direction, float impactForce)
    {
        float force = impactForce * explosiveForceMultiplier;
        Rigidbody otherRb = otherPlayer.GetComponent<Rigidbody>();

        // This car receives less force (was hit) and gets pushed backward
        rb.AddForceAtPosition(-direction * (force * 0.5f), collisionPoint, ForceMode.Impulse);
        rb.AddForce(Vector3.up * force * upwardModifier, ForceMode.Impulse);

        // Other car receives full force (was doing the hitting) and gets pushed forward
        otherRb.AddForceAtPosition(direction * force, collisionPoint, ForceMode.Impulse);
        otherRb.AddForce(Vector3.up * force * upwardModifier, ForceMode.Impulse);

        // Sync physics to clients for visual consistency
        SyncPhysicsForceClientRpc(NetworkObjectId, otherPlayer.NetworkObjectId, collisionPoint, -direction, direction, force);
    }

    [ClientRpc]
    private void SyncPhysicsForceClientRpc(ulong id1, ulong id2, Vector3 point, Vector3 dir1, Vector3 dir2, float force)
    {
        if (IsServer) 
            return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id1, out var obj1))
            obj1.GetComponent<Rigidbody>()?.AddForceAtPosition(dir1 * (force * 0.5f), point, ForceMode.Impulse);

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(id2, out var obj2))
            obj2.GetComponent<Rigidbody>()?.AddForceAtPosition(dir2 * force, point, ForceMode.Impulse);
    }
    #endregion
}
