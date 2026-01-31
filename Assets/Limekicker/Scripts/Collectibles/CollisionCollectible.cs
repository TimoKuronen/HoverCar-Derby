using System.Collections;
using Unity.Netcode;
using UnityEngine;

public abstract class CollisionCollectible : NetworkBehaviour
{
    [SerializeField] private GameObject visuals;

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
        // Reset processed state on enable (server only)
        if (IsServer)
        {
            isProcessed.Value = false;
        }
        
        if (visuals != null)
            visuals.SetActive(true);
        if (triggerCollider != null)
            triggerCollider.enabled = true;
    }

    private void OnCollisionEnter(Collision collidingCar)
    {
        // Only process on server to prevent duplicate collections
        if (!IsServer || isProcessed.Value || !collidingCar.gameObject.CompareTag("Vehicle"))
            return;

        float magnitude = collidingCar.relativeVelocity.magnitude;
        if (magnitude > 5)
        {
            ProcessItem(collidingCar);
        }
    }

    void ProcessItem(Collision collidingCar)
    {
        // Mark as processed (server-only)
        if (!IsServer)
            return;

        isProcessed.Value = true;

        var carManager = collidingCar.gameObject.GetComponent<CarManager>();
        if (carManager != null)
        {
            CollectItem(this, carManager);
        }
        
        StartCoroutine(PlayEffects());
    }

    private IEnumerator PlayEffects()
    {
        yield return new WaitForSeconds(2);

        //ReturnToPool();
    }

    protected abstract void CollectItem(CollisionCollectible collectible, CarManager carManager);
}
