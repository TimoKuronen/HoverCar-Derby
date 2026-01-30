using System;
using System.Collections;
using UnityEngine;

public class CarManager : MonoBehaviour
{
    [field: SerializeField] public CarData CarData { get; private set; }
    private CarDamageManager damageManager;

    public static event Action<CarManager> OnCarRespawned;
    public PlayerController PlayerController { get; private set; }

    private void Awake()
    {
        PlayerController = GetComponent<PlayerController>();
        damageManager = new CarDamageManager(this, PlayerController.NetworkObject, PlayerController);
        damageManager.OnCarDestroyed += HandleRespawn;

        if (TryGetComponent<CarVFX>(out var carVFX))
        {
            carVFX.Init(damageManager);
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            damageManager.ApplyDamageToPart(CarPartType.Hull, damageManager.CurrentCarHealth, transform.position);
            Debug.Log("Car destroyed for testing purposes." + PlayerController.PlayerName);
        }
    }

    private void HandleRespawn()
    {
        StartCoroutine(Respawn());
    }

    private IEnumerator Respawn()
    {
        yield return new WaitForSeconds(0.5f);
        damageManager.Repair(100f);
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
                damageManager.Repair(repair.RepairAmount);
                break;

            case DamagingCollectible damager:

                CarPartType[] parts = (CarPartType[])Enum.GetValues(typeof(CarPartType));
                int index = UnityEngine.Random.Range(0, parts.Length);

                damageManager.ApplyDamageToPart(parts[index], damager.DamageAmount, collectible.transform.position);
                break;

            default:
                Debug.LogWarning("Unknown collectible type!");
                break;
        }
    }

    private void OnDestroy()
    {
        damageManager.OnCarDestroyed -= HandleRespawn;
    }
}
