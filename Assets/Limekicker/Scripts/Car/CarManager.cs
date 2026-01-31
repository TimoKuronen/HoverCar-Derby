using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarManager : MonoBehaviour
{
    [field: SerializeField] public CarData CarData { get; private set; }
    public CarDamageManager DamageManager { get; private set; }

    public PlayerController PlayerController { get; private set; }
    public NetworkObject NetworkObject => PlayerController.NetworkObject;

    public static event Action<CarManager> OnCarRespawned;

    private void Awake()
    {
        PlayerController = GetComponent<PlayerController>();
        DamageManager = new CarDamageManager(this, PlayerController.NetworkObject, PlayerController);
        DamageManager.OnCarDestroyed += HandleRespawn;

        if (TryGetComponent<CarVFX>(out var carVFX))
        {
            carVFX.Init(DamageManager);
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
        StartCoroutine(Respawn());
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(0.5f);
        DamageManager.Repair(100f);
        OnCarRespawned?.Invoke(this);
        yield return new WaitForSeconds(0.5f);
        // wait for teleportation to finish
        yield return new WaitForSeconds(0.5f);

    }

    public void CollectItem(CollisionCollectible collectible)
    {
        switch (collectible)
        {
            case RepairCollectible repair:
                DamageManager.Repair(repair.RepairAmount);
                break;

            case DamagingCollectible damager:

                CarPartType[] parts = (CarPartType[])Enum.GetValues(typeof(CarPartType));
                int index = UnityEngine.Random.Range(0, parts.Length);

                DamageManager.ApplyDamageToPart(parts[index], damager.DamageAmount, collectible.transform.position);
                break;

            default:
                Debug.LogWarning("Unknown collectible type!");
                break;
        }
    }

    private void OnDestroy()
    {
        DamageManager.OnCarDestroyed -= HandleRespawn;
    }
}
