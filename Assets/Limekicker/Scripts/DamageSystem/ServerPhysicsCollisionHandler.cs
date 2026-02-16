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

    [Header("Collision Cooldown")]
    [SerializeField] private float collisionCooldown = 0.5f;

    private Rigidbody rb;
    private PlayerController playerController;
    private CarDamageManager damageManager;
    private float lastCollisionTime;
    private static HashSet<ulong> globalRecentCollisions = new HashSet<ulong>();
    #endregion

    #region Network Lifecycle
    public override void OnNetworkSpawn()
    {
        if (!IsServer) 
            enabled = false;
    }
    #endregion

    #region Unity Lifecycle
    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        playerController = GetComponent<PlayerController>();
        damageManager = playerController.DamageManager;
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (!IsServer || Time.time - lastCollisionTime < collisionCooldown) 
            return;

        PlayerController otherPlayer = collision.gameObject.GetComponent<PlayerController>();
        if (otherPlayer == null) 
            return;

        bool thisIsBot = playerController.IsBot;
        bool otherIsBot = otherPlayer.IsBot;

        ulong thisId = thisIsBot ? NetworkObjectId : OwnerClientId;
        ulong otherId = otherIsBot ? otherPlayer.NetworkObjectId : otherPlayer.OwnerClientId;

        ulong collisionKey = thisId < otherId ? (thisId << 32) | otherId : (otherId << 32) | thisId;

        if (globalRecentCollisions.Contains(collisionKey)) return;

        if (thisId > otherId) 
            return;

        globalRecentCollisions.Add(collisionKey);

        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce < minImpactForce)
        {
            globalRecentCollisions.Remove(collisionKey);
            return;
        }

        ProcessCarCollision(otherPlayer, collision.contacts[0].point, (otherPlayer.transform.position - transform.position).normalized, impactForce, collision);

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

    private void ProcessCarCollision(PlayerController otherPlayer, Vector3 collisionPoint, Vector3 direction, float impactForce, Collision collision)
    {
        Rigidbody otherRb = otherPlayer.GetComponent<Rigidbody>();
        if (otherRb == null) 
            return;

        Vector3 hitNormal = collision.contacts[0].normal;

        bool thisIsFrontBumper = Vector3.Dot(transform.forward, -hitNormal) > 0.7f;
        bool otherIsFrontBumper = Vector3.Dot(otherPlayer.transform.forward, hitNormal) > 0.7f;

        bool isHeadOn = thisIsFrontBumper && otherIsFrontBumper;
        float baseDamage = impactForce * damageMultiplier;

        float damageToThis = 0f;
        float damageToOther = 0f;

        if (isHeadOn)
        {
            float totalDamage = baseDamage * 0.5f;
            float thisSpeed = rb.velocity.magnitude;
            float otherSpeed = otherRb.velocity.magnitude;

            if (thisSpeed >= otherSpeed)
            {
                damageToOther = totalDamage * 0.6f;
                damageToThis = totalDamage * 0.4f;
            }
            else
            {
                damageToOther = totalDamage * 0.4f;
                damageToThis = totalDamage * 0.6f;
            }
        }
        else
        {
            if (thisIsFrontBumper)
            {
                damageToThis = 0f;
                damageToOther = baseDamage;
            }
            else if (otherIsFrontBumper)
            {
                damageToThis = baseDamage;
                damageToOther = 0f;
            }
            else
            {
                damageToThis = baseDamage * 0.5f;
                damageToOther = baseDamage * 0.5f;
            }
        }

        if (damageToThis > 0) 
            ApplyDamageToCar(otherPlayer, collision, damageToThis);

        if (damageToOther > minDamageThreshold)
        {
            ApplyDamageToOtherCar(otherPlayer, collision, damageToOther);
        }

        ApplyPhysicsForces(otherPlayer, collisionPoint, direction, impactForce);
    }

    private void ApplyDamageToCar(PlayerController otherPlayer, Collision collision, float damage)
    {
        if (damageManager == null) 
            return;

        ulong? attackerId = otherPlayer.IsBot ? otherPlayer.NetworkObjectId : otherPlayer.OwnerClientId;
        damageManager.ApplyDamage(damage, collision.contacts[0].point, attackerId);
        Debug.Log($"[ServerPhysicsCollisionHandler] Applied {damage} damage to {playerController.PlayerName.Value} from collision.");
    }

    private void ApplyDamageToOtherCar(PlayerController otherPlayer, Collision collision, float damage)
    {
        CarDamageManager otherDM = otherPlayer.DamageManager;
        if (otherDM != null)
        {
            ulong attackerId = playerController.IsBot ? NetworkObjectId : OwnerClientId;
            otherDM.ApplyDamage(damage, collision.contacts[0].point, attackerId);
            Debug.Log($"[ServerPhysicsCollisionHandler] Applied {damage} damage to {otherPlayer.PlayerName.Value} from collision with {playerController.PlayerName.Value}.");
        }
    }

    private void ApplyPhysicsForces(PlayerController otherPlayer, Vector3 collisionPoint, Vector3 direction, float impactForce)
    {
        float force = impactForce * explosiveForceMultiplier;
        Rigidbody otherRb = otherPlayer.GetComponent<Rigidbody>();

        rb.AddForceAtPosition(-direction * (force * 0.5f), collisionPoint, ForceMode.Impulse);
        rb.AddForce(Vector3.up * force * upwardModifier, ForceMode.Impulse);

        otherRb.AddForceAtPosition(direction * force, collisionPoint, ForceMode.Impulse);
        otherRb.AddForce(Vector3.up * force * upwardModifier, ForceMode.Impulse);

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
