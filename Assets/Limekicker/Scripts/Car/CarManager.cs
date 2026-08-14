using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Networked car lifecycle hub for respawn, VFX, and player linkage.
/// </summary>
public class CarManager : NetworkBehaviour
{
    [field: SerializeField] public CarData CarData { get; private set; }

    [Header("Respawn Settings")]
    [SerializeField] private float hopHeight = 3f;
    [SerializeField] private float hopDuration = 1f;

    private HoverCarControl hoverCarControl;
    private CarVFX carVFX;
    private Rigidbody carRigidbody;
    private EventBinding<CollectibleCollectedEvent> collectibleCollectedEvent;

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

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        PlayerController = GetComponent<PlayerController>();
        hoverCarControl = GetComponentInChildren<HoverCarControl>();
        carRigidbody = GetComponent<Rigidbody>();
        DamageManager = PlayerController.DamageManager;

        DamageManager.Initialize(PlayerController);
        DamageManager.OnCarDestroyed += HandleCarDestroyed;

        if (TryGetComponent<CarVFX>(out carVFX))
        {
            carVFX.Initialize(DamageManager);
        }

        if (IsServer)
        {
            collectibleCollectedEvent = new EventBinding<CollectibleCollectedEvent>(OnCollectibleCollected);
            EventBus<CollectibleCollectedEvent>.Register(collectibleCollectedEvent);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (collectibleCollectedEvent != null)
        {
            EventBus<CollectibleCollectedEvent>.Unregister(collectibleCollectedEvent);
        }

        DamageManager.OnCarDestroyed -= HandleCarDestroyed;
        base.OnNetworkDespawn();
    }

    public CarDamageManager DamageManager { get; private set; }
    public PlayerController PlayerController { get; private set; }

    public static event Action<CarManager, Action> OnCarRespawned;

    private void OnCollectibleCollected(CollectibleCollectedEvent collectedEvent)
    {
        if (collectedEvent.CollectorNetworkObjectId != NetworkObjectId)
            return;

        switch (collectedEvent.Type)
        {
            case CollectibleType.Repair:
                DamageManager.Repair(collectedEvent.Magnitude);
                break;

            case CollectibleType.Damage:
                DamageManager.ApplyDamage(collectedEvent.Magnitude, collectedEvent.WorldPosition);
                break;

            case CollectibleType.SpeedBoost:
                Debug.LogWarning("[CarManager] SpeedBoost collectible is not implemented yet.");
                break;
        }
    }

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

        if (carVFX != null)
        {
            carVFX.StopFireEffect();
        }

        yield return StartCoroutine(HopCarIntoAir());

        hoverCarControl.ToggleHovering(true);
    }

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
}
