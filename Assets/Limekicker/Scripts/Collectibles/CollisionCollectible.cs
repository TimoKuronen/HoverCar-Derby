using System.Collections;
using Unity.Netcode;
using UnityEngine;

public enum CollectibleType
{
    None,
    Repair,
    Points,
    Damage,
    SpeedBoost
}

public abstract class CollisionCollectible : NetworkBehaviour
{
    [SerializeField] private CollectibleType collectibleType;
    [SerializeField] private GameObject visuals;

    public CollectibleType CollectibleType => collectibleType;

    private Collider triggerCollider;
    private NetworkVariable<bool> isProcessed = new NetworkVariable<bool>(
        false,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        isProcessed.OnValueChanged += OnProcessedChanged;
    }

    public override void OnNetworkDespawn()
    {
        isProcessed.OnValueChanged -= OnProcessedChanged;
        base.OnNetworkDespawn();
    }

    private void OnProcessedChanged(bool oldValue, bool newValue)
    {
        if (newValue)
        {
            // Hide visuals when processed
            if (visuals != null)
                visuals.SetActive(false);
            if (triggerCollider != null)
                triggerCollider.enabled = false;
        }
    }

    private void Awake()
    {
        triggerCollider = GetComponent<Collider>();
    }

    private void OnEnable()
    {
        if (IsServer)
        {
            isProcessed.Value = false;
        }

        if (visuals != null)
            visuals.SetActive(true);
        if (triggerCollider != null)
            triggerCollider.enabled = true;
    }

    /// <summary>
    /// Handles collision with vehicles. Only processes on server and requires minimum impact velocity.
    /// </summary>
    private void OnCollisionEnter(Collision collidingCar)
    {
        if (!IsServer || isProcessed.Value || !collidingCar.gameObject.CompareTag("Vehicle"))
            return;

        float magnitude = collidingCar.relativeVelocity.magnitude;
        if (magnitude > 5)
        {
            ProcessItem(collidingCar);
        }
    }

    /// <summary>
    /// Processes collectible collection: marks as processed, applies effect to car, and notifies clients.
    /// </summary>
    private void ProcessItem(Collision collidingCar)
    {
        if (!IsServer)
            return;

        isProcessed.Value = true;

        var carManager = collidingCar.gameObject.GetComponent<CarManager>();
        if (carManager != null)
        {
            CollectItem(this, carManager);

            NetworkObject playerNetworkObject = collidingCar.gameObject.GetComponent<NetworkObject>();
            if (playerNetworkObject != null)
            {
                RaiseCollectibleCollectedEventClientRpc(playerNetworkObject.NetworkObjectId, (int)collectibleType);
            }
        }

        StartCoroutine(PlayEffects());
    }

    [ClientRpc]
    private void RaiseCollectibleCollectedEventClientRpc(ulong playerNetworkObjectId, int collectibleTypeInt)
    {
        EventBus<CollectibleCollectedEvent>.Raise(new CollectibleCollectedEvent
        {
            PlayerNetworkObjectId = playerNetworkObjectId,
            CollectibleType = (CollectibleType)collectibleTypeInt
        });
    }

    private IEnumerator PlayEffects()
    {
        yield return new WaitForSeconds(2);

        //ReturnToPool();
    }

    protected abstract void CollectItem(CollisionCollectible collectible, CarManager carManager);
}
