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

    public static event Action<CarManager> OnCarRespawned;

    private void Start()
    {
        PlayerController = GetComponent<PlayerController>();
        hoverCarControl = GetComponentInChildren<HoverCarControl>();
        DamageManager = PlayerController.DamageManager;    
        DamageManager.OnCarDestroyed += HandleRespawn;

        if (TryGetComponent<CarVFX>(out var carVFX))
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
                Debug.Log("Not the owner, cannot destroy the car for testing purposes.");
                return;
            }

            DamageManager.ApplyDamageToPart(CarPartType.Hull, DamageManager.CurrentCarHealth * 2, transform.position);
            Debug.Log("Car destroyed for testing purposes." + PlayerController.PlayerName.Value + " with this much health still left " + DamageManager.CurrentCarHealth);
        }
    }

    private void HandleRespawn()
    {
        // Only server should handle respawn
        if (IsServer)
        {
            StartCoroutine(Respawn());
        }
    }

    private IEnumerator Respawn()
    {
        hoverCarControl.ToggleHovering(false);

        yield return new WaitForSeconds(0.5f);

        OnCarRespawned?.Invoke(this);

        yield return new WaitForSeconds(0.5f);

        DamageManager.Repair(100f);
        // wait for teleportation to finish
        yield return new WaitForSeconds(0.5f);

        hoverCarControl.ToggleHovering(true);
        StartCoroutine(GameHUD.Instance.AnimateGoText());
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
        DamageManager.OnCarDestroyed -= HandleRespawn;
        base.OnNetworkDespawn();
    }
}
