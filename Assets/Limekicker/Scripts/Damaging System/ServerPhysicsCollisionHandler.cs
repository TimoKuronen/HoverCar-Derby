using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-side physics collision handler for car-to-car impacts.
/// Detects collisions, calculates impact forces, applies explosive forces to push cars apart,
/// and logs damage for health system integration.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(PlayerController))]
public class ServerPhysicsCollisionHandler : NetworkBehaviour
{
    [Header("Impact Settings")]
    [SerializeField] private float minImpactForce = 5f;
    [SerializeField] private float explosiveForceMultiplier = 1f;
    [SerializeField] private float explosiveRadius = 5f;
    [SerializeField] private float upwardModifier = 0.5f;
    
    [Header("Damage Settings")]
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private float minDamageThreshold = 5f;
    
    [Header("Collision Cooldown")]
    [SerializeField] private float collisionCooldown = 0.5f;
    
    private Rigidbody rb;
    private PlayerController playerController;
    private CarDamageManager damageManager;
    private float lastCollisionTime;
    private ulong lastCollisionTargetId;
    
    // Track collisions to prevent duplicate processing
    private System.Collections.Generic.HashSet<ulong> recentCollisions = new System.Collections.Generic.HashSet<ulong>();
    
    public override void OnNetworkSpawn()
    {
        rb = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();
        damageManager = GetComponent<CarDamageManager>();
        
        // Only enable collision detection on server
        if (!IsServer)
        {
            enabled = false;
            return;
        }
    }
    
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log("collision event 1");
        // Only process on server
        if (!IsServer)
            return;
        
        // Check if enough time has passed since last collision
        if (Time.time - lastCollisionTime < collisionCooldown)
            return;
        
        // Check if collision is with another player car
        PlayerController otherPlayer = collision.gameObject.GetComponent<PlayerController>();
        if (otherPlayer == null)
            return;

        Debug.Log("collision event 2");

        // Check if either car is a bot
        bool thisIsBot = GetComponent<BotPlayerController>() != null;
        bool otherIsBot = otherPlayer.GetComponent<BotPlayerController>() != null;
        
        // Prevent self-collision (but allow bot-to-bot and bot-to-player collisions)
        // Bots are owned by server, so they share the same OwnerClientId - we need to check by GameObject instead
        if (!thisIsBot && !otherIsBot && otherPlayer.OwnerClientId == OwnerClientId)
            return;
        
        // Prevent bot from colliding with itself (shouldn't happen, but safety check)
        if (thisIsBot && otherIsBot && gameObject == collision.gameObject)
            return;

        Debug.Log("collision event 3");

        // Prevent duplicate processing (both cars will detect the collision)
        // For bots, use NetworkObjectId instead of OwnerClientId since bots share server's client ID
        ulong thisId = thisIsBot ? GetComponent<NetworkObject>().NetworkObjectId : OwnerClientId;
        ulong otherId = otherIsBot ? otherPlayer.GetComponent<NetworkObject>().NetworkObjectId : otherPlayer.OwnerClientId;
        ulong collisionKey = thisId < otherId 
            ? (thisId << 32) | otherId 
            : (otherId << 32) | thisId;
        
        if (recentCollisions.Contains(collisionKey))
            return;
        
        recentCollisions.Add(collisionKey);
        
        // Calculate impact force
        float impactForce = collision.relativeVelocity.magnitude;
        
        if (impactForce < minImpactForce)
        {
            Debug.Log("collision event 4 didnt have enough force " + impactForce);
            recentCollisions.Remove(collisionKey);
            return;
        }

        // Get collision point and direction
        Vector3 collisionPoint = collision.contacts[0].point;
        Vector3 collisionNormal = collision.contacts[0].normal;
        
        // Calculate direction from this car to other car
        Vector3 directionToOther = (collision.gameObject.transform.position - transform.position).normalized;
        
        // Process collision for both cars
        ProcessCarCollision(otherPlayer, collisionPoint, directionToOther, impactForce, collision);
        
        // Update last collision time
        lastCollisionTime = Time.time;
        // Store the collision key instead of just the ID for better tracking
        lastCollisionTargetId = collisionKey;
        
        // Remove from recent collisions after cooldown
        StartCoroutine(RemoveFromRecentCollisions(collisionKey, collisionCooldown));
    }
    
    private IEnumerator RemoveFromRecentCollisions(ulong collisionKey, float delay)
    {
        yield return new WaitForSeconds(delay);
        recentCollisions.Remove(collisionKey);
    }
    
    private void ProcessCarCollision(PlayerController otherPlayer, Vector3 collisionPoint, Vector3 direction, float impactForce, Collision collision)
    {
        // Get other car's rigidbody
        Rigidbody otherRb = otherPlayer.GetComponent<Rigidbody>();
        if (otherRb == null)
            return;
        
        // Calculate damage based on impact force
        float damage = CalculateDamage(impactForce);
        
        // Log damage for both cars
        Debug.Log($"[ServerPhysicsCollisionHandler] Collision detected! " +
                  $"Player {OwnerClientId} hit Player {otherPlayer.OwnerClientId}. " +
                  $"Impact Force: {impactForce:F2}, Damage: {damage:F2}");
        
        // Apply damage to this car
        if (damageManager != null && damage > minDamageThreshold)
        {
            ApplyDamageToCar(collision, damage);
        }
        
        // Apply damage to other car
        CarDamageManager otherDamageManager = otherPlayer.GetComponent<CarDamageManager>();
        if (otherDamageManager != null && damage > minDamageThreshold)
        {
            ApplyDamageToOtherCar(otherPlayer, collision, damage);
        }
        
        // Calculate explosive force magnitude based on impact force
        float explosiveForce = impactForce * explosiveForceMultiplier;
        
        // Apply explosive force to push cars apart
        // Force is applied in opposite directions for each car
        Vector3 forceDirection = direction.normalized;
        Vector3 oppositeDirection = -forceDirection;
        
        // Apply force to this car (push away from other car)
        ApplyExplosiveForce(rb, collisionPoint, oppositeDirection, explosiveForce);
        
        // Apply force to other car (push away from this car)
        ApplyExplosiveForce(otherRb, collisionPoint, forceDirection, explosiveForce);

        Debug.Log("final collision event with force of " + explosiveForce);

        // Sync physics forces to all clients via RPC
        NetworkObject thisNetworkObj = GetComponent<NetworkObject>();
        NetworkObject otherNetworkObj = otherPlayer.GetComponent<NetworkObject>();
        
        if (thisNetworkObj != null && otherNetworkObj != null)
        {
            SyncPhysicsForceClientRpc(
                thisNetworkObj.NetworkObjectId,
                otherNetworkObj.NetworkObjectId,
                collisionPoint,
                oppositeDirection,
                forceDirection,
                explosiveForce
            );
        }
    }
    
    private float CalculateDamage(float impactForce)
    {
        // Damage calculation: scale impact force by damage multiplier
        // You can adjust this formula based on your game's needs
        return impactForce * damageMultiplier;
    }
    
    private void ApplyDamageToCar(Collision collision, float damage)
    {
        if (damageManager == null)
            return;
        
        Vector3 hitDirection = collision.contacts[0].normal;
        float forwardDot = Vector3.Dot(hitDirection, transform.forward);
        float rightDot = Vector3.Dot(hitDirection, transform.right);
        
        // Determine which part was hit based on collision direction
        CarPartType partType = CarPartType.FrontBumper; // default
        
        if (forwardDot > 0.8f)
        {
            partType = CarPartType.FrontBumper;
            damageManager.ApplyDamageToPart(partType, damage * 2f);
        }
        else if (rightDot > 0.5f)
        {
            partType = CarPartType.SidePanel_Right;
            damageManager.ApplyDamageToPart(partType, damage * 1.2f);
        }
        else if (rightDot < -0.5f)
        {
            partType = CarPartType.SidePanel_Left;
            damageManager.ApplyDamageToPart(partType, damage * 1.2f);
        }
        else
        {
            partType = CarPartType.RearBumper;
            damageManager.ApplyDamageToPart(partType, damage * 0.8f);
        }
    }
    
    private void ApplyDamageToOtherCar(PlayerController otherPlayer, Collision collision, float damage)
    {
        CarDamageManager otherDamageManager = otherPlayer.GetComponent<CarDamageManager>();
        if (otherDamageManager == null)
            return;
        
        Transform otherTransform = otherPlayer.transform;
        Vector3 hitDirection = collision.contacts[0].normal;
        float forwardDot = Vector3.Dot(hitDirection, otherTransform.forward);
        float rightDot = Vector3.Dot(hitDirection, otherTransform.right);
        
        // Determine which part was hit based on collision direction (from other car's perspective)
        CarPartType partType = CarPartType.FrontBumper; // default
        
        if (forwardDot > 0.8f)
        {
            partType = CarPartType.FrontBumper;
            otherDamageManager.ApplyDamageToPart(partType, damage * 2f);
        }
        else if (rightDot > 0.5f)
        {
            partType = CarPartType.SidePanel_Right;
            otherDamageManager.ApplyDamageToPart(partType, damage * 1.2f);
        }
        else if (rightDot < -0.5f)
        {
            partType = CarPartType.SidePanel_Left;
            otherDamageManager.ApplyDamageToPart(partType, damage * 1.2f);
        }
        else
        {
            partType = CarPartType.RearBumper;
            otherDamageManager.ApplyDamageToPart(partType, damage * 0.8f);
        }
    }
    
    private void ApplyExplosiveForce(Rigidbody targetRb, Vector3 position, Vector3 direction, float force)
    {
        if (targetRb == null)
            return;
        
        // Apply force in the specified direction
        // Using AddForceAtPosition for more realistic physics
        targetRb.AddForceAtPosition(direction * force, position, ForceMode.Impulse);
        
        // Also add some upward force for more dramatic effect
        targetRb.AddForceAtPosition(Vector3.up * force * upwardModifier, position, ForceMode.Impulse);
    }
    
    /// <summary>
    /// ClientRpc to sync physics forces to all clients.
    /// This ensures both players see the collision effects.
    /// Note: Server applies forces immediately, this RPC syncs to clients.
    /// </summary>
    [ClientRpc]
    private void SyncPhysicsForceClientRpc(
        ulong thisCarNetworkId,
        ulong otherCarNetworkId,
        Vector3 collisionPoint,
        Vector3 thisCarDirection,
        Vector3 otherCarDirection,
        float force)
    {
        // Server already applied forces in ProcessCarCollision, so skip here
        // This RPC is only for pure clients to apply the forces
        if (IsServer)
            return;
        
        // Find the network objects
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(thisCarNetworkId, out NetworkObject thisCarObj))
        {
            Debug.LogWarning($"[ServerPhysicsCollisionHandler] Could not find network object {thisCarNetworkId} for force sync");
            return;
        }
        
        if (!NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(otherCarNetworkId, out NetworkObject otherCarObj))
        {
            Debug.LogWarning($"[ServerPhysicsCollisionHandler] Could not find network object {otherCarNetworkId} for force sync");
            return;
        }
        
        // Get rigidbodies
        Rigidbody thisRb = thisCarObj.GetComponent<Rigidbody>();
        Rigidbody otherRb = otherCarObj.GetComponent<Rigidbody>();
        
        if (thisRb != null)
        {
            ApplyExplosiveForce(thisRb, collisionPoint, thisCarDirection, force);
        }
        
        if (otherRb != null)
        {
            ApplyExplosiveForce(otherRb, collisionPoint, otherCarDirection, force);
        }
    }
}

