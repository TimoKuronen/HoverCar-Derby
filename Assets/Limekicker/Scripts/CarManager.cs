using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarManager : MonoBehaviour
{
    private CarDamageManager damageManager;

    internal void CollectItem(CollisionCollectible collectible)
    {
        switch (collectible)
        {
            case RepairCollectible repair:
                damageManager.Repair(repair.RepairAmount);
                break;

            case DamagingCollectible damager:

                CarPartType[] parts = (CarPartType[])Enum.GetValues(typeof(CarPartType));
                int index = UnityEngine.Random.Range(0, parts.Length);

                damageManager.ApplyDamageToPart(parts[index], damager.DamageAmount);
                break;

            default:
                Debug.LogWarning("Unknown collectible type!");
                break;
        }
    }

    private void Awake()
    {
        damageManager = GetComponent<CarDamageManager>();
    }
}
