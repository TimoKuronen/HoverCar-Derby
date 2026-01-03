using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class CarDamageManager : MonoBehaviour
{
    private CarManager carManager;
    private NetworkObject networkObject;
    private float currentCarHealth;
    private float maxCarHealth;

    public PlayerController PlayerController { get; private set; }
    public float CarHealthPercentage => (currentCarHealth / maxCarHealth) * 100f;

    // Currently not in use
    public Dictionary<CarPartType, CarPart> CarParts { get; private set; } = new Dictionary<CarPartType, CarPart>();

    public event Action OnCarDestroyed;
    public event Action<float, Vector3> OnCarDamaged;

    private void Start()
    {
        carManager = GetComponent<CarManager>();
        networkObject = GetComponent<NetworkObject>();
        PlayerController = GetComponent<PlayerController>();

        if (TryGetComponent<CarVFX>(out var carVFX))
        {
            carVFX.Init(this);
        }

        currentCarHealth = 100f;
        maxCarHealth = currentCarHealth;
    }

    public void ApplyDamageToPart(CarPartType partType, float damage, Vector3 damagePosition, ulong? attackerClientId = null)
    {
        ulong attackerId = attackerClientId ?? ulong.MaxValue;
        ulong victimId = ulong.MaxValue;

        if (PlayerController != null && networkObject != null)
        {
            victimId = PlayerController.OwnerClientId;
        }

        float damageDealt = damage * GetDamageReductionMultiplier(partType);
        currentCarHealth -= damageDealt;

        OnCarDamaged.Invoke(damageDealt, damagePosition);

        ShowDamageNumber(damagePosition, damageDealt, attackerId, victimId);

        if (currentCarHealth <= 0)
        {
            OnCarDestroyed?.Invoke();
        }
    }

    /// <summary>
    /// Shows a damage number for this car.
    /// </summary>
    private void ShowDamageNumber(Vector3 worldPosition, float damageAmount, ulong attackerClientId, ulong victimClientId)
    {
        DamageNumberPool.Instance.ShowDamageNumber(worldPosition, damageAmount, attackerClientId, victimClientId);
    }

    public void Repair(float amount)
    {
        float[] currentPartHealth = new float[CarParts.Count];
        int index = 0;
        foreach (var item in CarParts)
        {
            currentPartHealth[index] = item.Value.CurrentHealth.Value;
            index++;
        }

        float[] repairValues = DistributeValueWithClamp(currentPartHealth, amount, 100);
        index = 0;

        foreach (var item in CarParts)
        {
            item.Value.RepairPart(repairValues[index]);
            index++;
        }
    }

    private float[] DistributeValueWithClamp(float[] array, float additionValue, float maxLimit = 100f)
    {
        if (array == null || array.Length == 0)
            throw new System.ArgumentException("Array cannot be null or empty");

        float[] result = (float[])array.Clone();
        float remainingAddition = additionValue;

        while (remainingAddition > 0)
        {
            // Find min & max for weighting
            float minValue = result.Where(val => val < maxLimit).DefaultIfEmpty(maxLimit).Min();
            float maxValue = result.Where(val => val < maxLimit).DefaultIfEmpty(maxLimit).Max();

            // If all values are already at maxLimit, break
            if (minValue >= maxLimit)
                break;

            // Calculate weights inversely
            float totalWeight = 0;
            float[] weights = new float[result.Length];

            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] < maxLimit) // Only consider values below max
                {
                    weights[i] = maxValue - result[i]; // Lower values get higher weight
                    totalWeight += weights[i];
                }
            }

            if (totalWeight == 0)
                break;

            // Distribute the addition
            float remainingBeforeLoop = remainingAddition;
            for (int i = 0; i < result.Length; i++)
            {
                if (result[i] < maxLimit)
                {
                    float distributedValue = (weights[i] / totalWeight) * remainingAddition;
                    result[i] += distributedValue;

                    // Clamp to maxLimit
                    if (result[i] > maxLimit)
                    {
                        remainingAddition -= (result[i] - maxLimit); // Reduce remainingAddition
                        result[i] = maxLimit; // Cap at maxLimit
                    }
                }
            }

            // If no value changed, stop (prevents infinite loops)
            if (Mathf.Approximately(remainingBeforeLoop, remainingAddition))
                break;
        }

        return result;
    }

    float GetDamageReductionMultiplier(CarPartType carPartHit)
    {
        return carPartHit switch
        {
            CarPartType.FrontBumper => 0.8f,
            CarPartType.SidePanel_Left => 0.3f,
            CarPartType.SidePanel_Right => 0.3f,
            CarPartType.RearBumper => 0.5f,
            _ => 0.5f,
        };
    }
}
