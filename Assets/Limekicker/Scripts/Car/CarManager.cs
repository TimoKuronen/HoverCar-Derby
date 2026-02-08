using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarManager : NetworkBehaviour
{
    [field: SerializeField] public CarData CarData { get; private set; }
    public CarDamageManager DamageManager { get; private set; }

    public PlayerController PlayerController { get; private set; }

    private HoverCarControl hoverCarControl;
    private CarVFX carVFX;
    private Rigidbody carRigidbody;

    [Header("Respawn Settings")]
    [SerializeField] private float hopHeight = 3f;
    [SerializeField] private float hopDuration = 1f;

    public static event Action<CarManager> OnCarRespawned;

    private void Start()
    {
        PlayerController = GetComponent<PlayerController>();
        hoverCarControl = GetComponentInChildren<HoverCarControl>();
        carRigidbody = GetComponent<Rigidbody>();
        DamageManager = PlayerController.DamageManager;    
        DamageManager.OnCarDestroyed += HandleCarDestroyed;

        if (TryGetComponent<CarVFX>(out carVFX))
        {
            carVFX.Initialize(DamageManager);
        }
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Initialize damage manager after network spawn
        if (PlayerController != null && DamageManager != null)
        {
            DamageManager.Initialize(this, PlayerController);
        }
    }

    private void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (!NetworkObject.IsOwner || PlayerController.IsBot)
            {
                return;
            }

            DamageManager.ApplyDamageToPart(CarPartType.Hull, DamageManager.CurrentCarHealth * 2, transform.position);
            Debug.Log("Car destroyed for testing purposes." + PlayerController.PlayerName.Value + " with this much health still left " + DamageManager.CurrentCarHealth);
        }
    }

    private void HandleCarDestroyed()
    {
        // Stop hovering ability immediately when destroyed
        if (hoverCarControl != null)
        {
            hoverCarControl.ToggleHovering(false);
        }

        // Enable fire effect when car is destroyed
        if (carVFX != null)
        {
            carVFX.EnableFireEffect();
        }

        // Only server should handle respawn
        if (IsServer)
        {
            StartCoroutine(Respawn());
        }
    }

    private IEnumerator Respawn()
    {
        // Wait for destruction effects to be visible
        yield return new WaitForSeconds(2f);

        Vector3 positionBeforeTeleport = transform.position;

        OnCarRespawned?.Invoke(this);

        // Wait for teleportation to actually complete
        // Check if position has changed (teleportation happened)
        float teleportWaitTime = 0f;
        float maxTeleportWait = 1.5f;

        while (Vector3.Distance(transform.position, positionBeforeTeleport) < 0.5f && teleportWaitTime < maxTeleportWait)
        {
            yield return new WaitForSeconds(0.1f);
            teleportWaitTime += 0.1f;
        }

        // Give a bit more time for NetworkTransform to fully sync across network
        yield return new WaitForSeconds(0.3f);

        // Notify camera to update immediately to new position via event
        if (IsOwner)
        {
            EventBus<PlayerTeleportedEvent>.Raise(new PlayerTeleportedEvent { NetworkObject = NetworkObject });
        }

        // Repair the car
        DamageManager.Repair(100f);
        carVFX.StopFireEffect();

        // Perform a hop to get the car in the air before re-enabling hovering
        yield return StartCoroutine(HopCarIntoAir());

        hoverCarControl.ToggleHovering(true);

        StartCoroutine(GameHUD.Instance.AnimateGoText());
    }

    private IEnumerator HopCarIntoAir()
    {
        if (carRigidbody == null)
        {
            Debug.LogWarning("[CarManager] Rigidbody not found, cannot perform hop");
            yield break;
        }

        // Store original position
        Vector3 originalPosition = transform.position;
        Vector3 targetPosition = originalPosition + Vector3.up * hopHeight;

        // Make rigidbody kinematic temporarily for non-physics movement
        bool wasKinematic = carRigidbody.isKinematic;
        carRigidbody.isKinematic = true;
        carRigidbody.velocity = Vector3.zero;
        carRigidbody.angularVelocity = Vector3.zero;

        // Smoothly lerp car to hop height with ease-out curve (fast start, slow end)
        float elapsedTime = 0f;
        while (elapsedTime < hopDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / hopDuration;
            float easedT = 1f - Mathf.Pow(1f - t, 3f);
            
            Vector3 currentPosition = Vector3.Lerp(originalPosition, targetPosition, easedT);
            
            carRigidbody.position = currentPosition;
            
            yield return null;
        }

        // Ensure we reach exactly the target position
        transform.position = targetPosition;
        carRigidbody.position = targetPosition;

        // Restore rigidbody physics
        carRigidbody.isKinematic = wasKinematic;
        
        // Reset velocity to prevent unwanted movement
        carRigidbody.velocity = Vector3.zero;
        carRigidbody.angularVelocity = Vector3.zero;
        
        // Sync transforms to ensure physics state is correct
        Physics.SyncTransforms();
    }

    public void CollectItem(CollisionCollectible collectible)
    {
        // If called from client, route to server
        if (!IsServer)
        {
            CollectItemServerRpc(collectible.NetworkObjectId);
            return;
        }

        // Server-side processing
        ProcessCollectible(collectible);
    }

    [ServerRpc(RequireOwnership = false)]
    private void CollectItemServerRpc(ulong collectibleNetworkObjectId)
    {
        if (!IsServer)
            return;

        // Find the collectible by NetworkObjectId
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(collectibleNetworkObjectId, out var networkObject))
        {
            var collectible = networkObject.GetComponent<CollisionCollectible>();
            if (collectible != null)
            {
                ProcessCollectible(collectible);
            }
        }
    }

    private void ProcessCollectible(CollisionCollectible collectible)
    {
        switch (collectible)
        {
            case RepairCollectible repair:
                DamageManager.Repair(repair.RepairAmount);
                break;

            case DamagingCollectible damager:
                DamageManager.ApplyDamageToPart(CarPartType.Hull, damager.DamageAmount, collectible.transform.position);
                break;

            default:
                Debug.LogWarning("Unknown collectible type!");
                break;
        }
    }

    public override void OnNetworkDespawn()
    {
        DamageManager.OnCarDestroyed -= HandleCarDestroyed;
        base.OnNetworkDespawn();
    }
}
