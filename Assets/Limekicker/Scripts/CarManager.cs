using System;
using UnityEngine;

public class CarManager : MonoBehaviour
{
    [SerializeField] private CarData carData;
    private CarDamageManager damageManager;

    public CarData CarData => carData;

    private void Awake()
    {
        damageManager = GetComponent<CarDamageManager>();
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
}
