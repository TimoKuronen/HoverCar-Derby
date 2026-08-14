using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Categories of pickup effects applied when a collectible is collected.
/// </summary>
public enum CollectibleType
{
    None,
    Repair,
    Points,
    Damage,
    SpeedBoost
}

/// <summary>
/// Serializable effect parameters for a collectible instance.
/// </summary>
[Serializable]
public struct CollectibleData
{
    public CollectibleType Type;
    public float Magnitude;
    public float HazardLifetimeSeconds;
}

/// <summary>
/// Networked pickup that applies configured effects on trigger collision.
/// </summary>
public class CollisionCollectible : NetworkBehaviour
{
    [SerializeField] private CollectibleData collectibleEffectData;
    [SerializeField] private GameObject visualsContainer;
    [SerializeField] private Collider triggerCollider;

    public CollectibleData CollectibleEffectData => collectibleEffectData;

    private NetworkVariable<bool> isProcessed = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isProcessed.OnValueChanged += OnProcessedChanged;

        if (IsServer && collectibleEffectData.HazardLifetimeSeconds > 0f)
        {
            StartCoroutine(DestroyAfterLifetime(collectibleEffectData.HazardLifetimeSeconds));
        }
    }

    public override void OnNetworkDespawn()
    {
        isProcessed.OnValueChanged -= OnProcessedChanged;
        base.OnNetworkDespawn();
    }

    private void OnProcessedChanged(bool oldValue, bool newValue)
    {
        if (!newValue)
            return;

        if (visualsContainer != null)
            visualsContainer.SetActive(false);

        if (triggerCollider != null)
            triggerCollider.enabled = false;
    }

    private void OnEnable()
    {
        if (IsServer)
        {
            isProcessed.Value = false;
        }

        if (visualsContainer != null)
            visualsContainer.SetActive(true);

        if (triggerCollider != null)
            triggerCollider.enabled = true;
    }

    private void OnCollisionEnter(Collision collidingCar)
    {
        if (!IsServer || isProcessed.Value || !collidingCar.gameObject.CompareTag("Vehicle"))
            return;

        if (collidingCar.relativeVelocity.magnitude <= 5f)
            return;

        ProcessItem(collidingCar);
    }

    private void ProcessItem(Collision collidingCar)
    {
        if (!IsServer)
            return;

        if (!collidingCar.gameObject.TryGetComponent<NetworkObject>(out NetworkObject collectorNetworkObject))
            return;

        isProcessed.Value = true;

        CollectibleCollectedEvent collectedEvent = new CollectibleCollectedEvent
        {
            CollectorNetworkObjectId = collectorNetworkObject.NetworkObjectId,
            Type = collectibleEffectData.Type,
            Magnitude = collectibleEffectData.Magnitude,
            WorldPosition = transform.position
        };

        EventBus<CollectibleCollectedEvent>.Raise(collectedEvent);

        RaiseCollectibleCollectedEventClientRpc(
            collectedEvent.CollectorNetworkObjectId,
            (int)collectedEvent.Type,
            collectedEvent.Magnitude,
            collectedEvent.WorldPosition);

        StartCoroutine(PlayEffects());
    }

    [ClientRpc]
    private void RaiseCollectibleCollectedEventClientRpc(
        ulong collectorNetworkObjectId,
        int collectibleType,
        float magnitude,
        Vector3 worldPosition)
    {
        if (IsServer)
            return;

        EventBus<CollectibleCollectedEvent>.Raise(new CollectibleCollectedEvent
        {
            CollectorNetworkObjectId = collectorNetworkObjectId,
            Type = (CollectibleType)collectibleType,
            Magnitude = magnitude,
            WorldPosition = worldPosition
        });
    }

    private IEnumerator PlayEffects()
    {
        yield return new WaitForSeconds(2f);

        //ReturnToPool();
    }

    private IEnumerator DestroyAfterLifetime(float lifetimeSeconds)
    {
        yield return new WaitForSeconds(lifetimeSeconds);

        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }
}
