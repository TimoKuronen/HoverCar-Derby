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
    Damage
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
    public bool IsProcessed => isProcessed.Value;

    private const float CollectedDespawnDelaySeconds = 2f;

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
            StartCoroutine(DespawnAfterDelay(collectibleEffectData.HazardLifetimeSeconds));
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

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer || isProcessed.Value)
            return;

        Rigidbody body = other.attachedRigidbody;
        if (body == null || !body.CompareTag("Vehicle"))
            return;

        ProcessItem(body.gameObject);
    }

    private void ProcessItem(GameObject collector)
    {
        if (!IsServer)
            return;

        if (!collector.TryGetComponent<NetworkObject>(out NetworkObject collectorNetworkObject))
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

        // Delay keeps the object alive long enough for the ClientRpc and pickup feedback to play out.
        StartCoroutine(DespawnAfterDelay(CollectedDespawnDelaySeconds));
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

    private IEnumerator DespawnAfterDelay(float delaySeconds)
    {
        yield return new WaitForSeconds(delaySeconds);

        if (IsServer && NetworkObject != null && NetworkObject.IsSpawned)
        {
            NetworkObject.Despawn();
        }
    }
}
