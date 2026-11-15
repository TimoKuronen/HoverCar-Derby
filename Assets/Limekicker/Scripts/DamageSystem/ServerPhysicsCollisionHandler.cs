using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-side car collision detection, damage rules, and physics impulse application.
/// </summary>
[RequireComponent(typeof(Rigidbody), typeof(PlayerController))]
public class ServerPhysicsCollisionHandler : NetworkBehaviour
{
    [Header("Impact Settings")]
    [SerializeField] private float minImpactForce = 5f;
    [SerializeField] private float explosiveForceMultiplier = 1f;
    [SerializeField] private float upwardModifier = 0.5f;

    [Header("Damage Settings")]
    [SerializeField] private float damageMultiplier = 1f;
    [SerializeField] private float minDamageThreshold = 5f;

    [Header("Velocity Thresholds")]
    [SerializeField] private float idleSpeedThreshold = 2f;
    [SerializeField] private float decentSpeedThreshold = 8f;

    [Header("Collision Cooldown")]
    [SerializeField] private float collisionCooldown = 0.5f;

    private Rigidbody rb;
    private PlayerController playerController;
    private CarDamageManager damageManager;
    private float lastCollisionTime;
    private static readonly CollisionDeduplicator recentCollisions = new CollisionDeduplicator();

    /// <summary>
    /// Speed sampled before the physics step. Collision callbacks run after the solver has already
    /// changed velocities, so live Rigidbody speeds make a rammed idle car look fast.
    /// </summary>
    public float PreStepSpeed { get; private set; }

    private void FixedUpdate()
    {
        if (rb != null)
            PreStepSpeed = rb.linearVelocity.magnitude;
    }

    private static float GetPreStepSpeed(PlayerController player)
    {
        return player.TryGetComponent(out ServerPhysicsCollisionHandler handler) ? handler.PreStepSpeed : 0f;
    }

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

        if (recentCollisions.IsRecent(collisionKey, Time.time, collisionCooldown))
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
            PreStepSpeed,
            GetPreStepSpeed(otherPlayer));

        lastCollisionTime = Time.time;
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
            PreStepSpeed,
            GetPreStepSpeed(otherPlayer));

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

        if (recentCollisions.IsRecent(collisionKey, Time.time, collisionCooldown))
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
    }

    private static void GetCollisionIds(PlayerController otherPlayer, PlayerController thisPlayer, NetworkBehaviour thisNetworkBehaviour, out ulong thisId, out ulong otherId, out ulong collisionKey)
    {
        bool thisIsBot = thisPlayer.IsBot;
        bool otherIsBot = otherPlayer.IsBot;

        thisId = thisIsBot ? thisNetworkBehaviour.NetworkObjectId : thisNetworkBehaviour.OwnerClientId;
        otherId = otherIsBot ? otherPlayer.NetworkObjectId : otherPlayer.OwnerClientId;
        collisionKey = CollisionDeduplicator.KeyFor(thisId, otherId);
    }

    private void GetCollisionIds(PlayerController otherPlayer, out ulong thisId, out ulong otherId, out ulong collisionKey)
    {
        GetCollisionIds(otherPlayer, playerController, this, out thisId, out otherId, out collisionKey);
    }

    private bool TryRegisterCollision(ulong collisionKey, float impactForce)
    {
        if (impactForce < minImpactForce)
            return false;

        recentCollisions.Register(collisionKey, Time.time, collisionCooldown);
        return true;
    }

    /// <summary>
    /// Applies the damage decided by <see cref="CollisionDamageCalculator"/> and the physics forces for one collision.
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
        CollisionDamage damage = CollisionDamageCalculator.Calculate(
            new CollisionInput
            {
                ThisForward = transform.forward,
                ThisRight = transform.right,
                OtherForward = otherPlayer.transform.forward,
                OtherRight = otherPlayer.transform.right,
                HitNormal = hitNormal,
                ThisSpeed = thisSpeed,
                OtherSpeed = otherSpeed,
                ImpactForce = impactForce
            },
            new DamageSettings
            {
                DamageMultiplier = damageMultiplier,
                MinDamageThreshold = minDamageThreshold,
                IdleSpeedThreshold = idleSpeedThreshold,
                DecentSpeedThreshold = decentSpeedThreshold
            });

        if (damage.ToThis > 0f)
            ApplyDamageToCar(otherPlayer, contactPoint, damage.ToThis);

        if (damage.ToOther > 0f)
            ApplyDamageToOtherCar(otherPlayer, contactPoint, damage.ToOther);

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
}
