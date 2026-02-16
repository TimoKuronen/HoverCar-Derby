using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarManager : NetworkBehaviour
{
    #region Fields
    [field: SerializeField] public CarData CarData { get; private set; }
    
    [Header("Respawn Settings")]
    [SerializeField] private float hopHeight = 3f;
    [SerializeField] private float hopDuration = 1f;

    private HoverCarControl hoverCarControl;
    private CarVFX carVFX;
    private Rigidbody carRigidbody;
    #endregion

    #region Properties
    public CarDamageManager DamageManager { get; private set; }
    public PlayerController PlayerController { get; private set; }
    #endregion

    #region Events
    public static event Action<CarManager, Action> OnCarRespawned;
    #endregion

    #region Unity Lifecycle
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

    private void Update()
    {
        if (Keyboard.current.rKey.wasPressedThisFrame)
        {
            if (!NetworkObject.IsOwner || PlayerController.IsBot)
            {
                return;
            }

            DamageManager.ApplyDamage(DamageManager.CurrentCarHealth * 2, transform.position);
            Debug.Log("Car destroyed for testing purposes." + PlayerController.PlayerName.Value + " with this much health still left " + DamageManager.CurrentCarHealth);
        }
    }
    #endregion

    #region Network Lifecycle
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        if (PlayerController != null && DamageManager != null)
        {
            DamageManager.Initialize(this, PlayerController);
        }
    }

    public override void OnNetworkDespawn()
    {
        DamageManager.OnCarDestroyed -= HandleCarDestroyed;
        base.OnNetworkDespawn();
    }
    #endregion

    #region Public Methods
    public void CollectItem(CollisionCollectible collectible)
    {
        if (!IsServer)
        {
            CollectItemServerRpc(collectible.NetworkObjectId);
            return;
        }

        ProcessCollectible(collectible);
    }
    #endregion

    #region Private Methods
    private void HandleCarDestroyed()
    {
        if (hoverCarControl != null)
        {
            hoverCarControl.ToggleHovering(false);
        }

        if (carVFX != null)
        {
            carVFX.EnableFireEffect();
        }

        if (IsServer)
        {
            StartCoroutine(Respawn());
        }
    }

    /// <summary>
    /// Handles car respawn sequence: waits for destruction effects, teleports to spawn point,
    /// repairs health, stops fire VFX, performs hop animation, and re-enables hovering.
    /// </summary>
    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(2f);

        bool teleportComplete = false;

        OnCarRespawned?.Invoke(this, () => teleportComplete = true);

        yield return new WaitUntil(() => teleportComplete);

        yield return new WaitForSeconds(0.1f);

        if (IsOwner)
        {
            EventBus<PlayerTeleportedEvent>.Raise(new PlayerTeleportedEvent { NetworkObject = NetworkObject });
        }

        DamageManager.Repair(100f);
        carVFX.StopFireEffect();

        yield return StartCoroutine(HopCarIntoAir());

        hoverCarControl.ToggleHovering(true);
    }

    /// <summary>
    /// Animates car hopping into the air after respawn. Uses easing for smooth motion.
    /// Temporarily makes rigidbody kinematic to control position directly.
    /// </summary>
    private IEnumerator HopCarIntoAir()
    {
        if (carRigidbody == null)
        {
            Debug.LogWarning("[CarManager] Rigidbody not found, cannot perform hop");
            yield break;
        }

        Vector3 originalPosition = transform.position;
        Vector3 targetPosition = originalPosition + Vector3.up * hopHeight;

        bool wasKinematic = carRigidbody.isKinematic;
        carRigidbody.isKinematic = true;

        // Animate hop with easing (ease-out cubic)
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

        transform.position = targetPosition;
        carRigidbody.position = targetPosition;
        carRigidbody.isKinematic = wasKinematic;
        carRigidbody.velocity = Vector3.zero;
        carRigidbody.angularVelocity = Vector3.zero;

        Physics.SyncTransforms();
    }

    [ServerRpc(RequireOwnership = false)]
    private void CollectItemServerRpc(ulong collectibleNetworkObjectId)
    {
        if (!IsServer)
            return;

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(collectibleNetworkObjectId, out var networkObject))
        {
            var collectible = networkObject.GetComponent<CollisionCollectible>();
            if (collectible != null)
            {
                ProcessCollectible(collectible);
            }
        }
    }

    /// <summary>
    /// Processes collectible collision based on type. Handles repair and damage collectibles.
    /// </summary>
    private void ProcessCollectible(CollisionCollectible collectible)
    {
        switch (collectible)
        {
            case RepairCollectible repair:
                DamageManager.Repair(repair.RepairAmount);
                break;

            case DamagingCollectible damager:
                DamageManager.ApplyDamage(damager.DamageAmount, collectible.transform.position);
                break;

            default:
                Debug.LogWarning("Unknown collectible type!");
                break;
        }
    }
    #endregion
}
