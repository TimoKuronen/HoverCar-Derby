using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class CarDamageManager : NetworkBehaviour
{    
    private NetworkVariable<float> currentCarHealth = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    
    private CarManager carManager;

    private readonly float maxCarHealth = 100f;

    public PlayerController PlayerController { get; private set; }
    public float CarHealthPercentage => currentCarHealth.Value / maxCarHealth * 100f;
    public float CurrentCarHealth => currentCarHealth.Value;

    // Currently not in use
    public Dictionary<CarPartType, CarPart> CarParts { get; private set; } = new Dictionary<CarPartType, CarPart>();

    public event Action OnCarDestroyed;
    public event Action<Vector3> OnCarDamaged;

    public void Initialize(CarManager carManager, PlayerController playerController)
    {
        this.carManager = carManager;
        this.PlayerController = playerController;
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Initialize health NetworkVariable if not already set
        if (IsServer && currentCarHealth.Value == 0)
        {
            currentCarHealth.Value = maxCarHealth;
        }

        // Subscribe to health changes
        currentCarHealth.OnValueChanged += OnHealthChanged;
        
        // Initialize health on server
        if (IsServer && currentCarHealth.Value == 0)
        {
            currentCarHealth.Value = maxCarHealth;
        }
    }

    public override void OnNetworkDespawn()
    {
        currentCarHealth.OnValueChanged -= OnHealthChanged;
        base.OnNetworkDespawn();
    }

    public void ApplyDamageToPart(CarPartType partType, float damage, Vector3 damagePosition, ulong? attackerClientId = null)
    {
        // Only server can apply damage
        if (!IsServer)
        {
            Debug.LogWarning("[CarDamageManager] ApplyDamageToPart called on client - use ServerRpc instead");
            return;
        }

        ulong attackerId = attackerClientId ?? ulong.MaxValue;
        ulong victimId = ulong.MaxValue;

        if (PlayerController != null && NetworkObject != null)
        {
            victimId = PlayerController.OwnerClientId;
        }

        float damageDealt = damage * GetDamageReductionMultiplier(partType);
        float newHealth = Mathf.Max(0f, currentCarHealth.Value - damageDealt);
        currentCarHealth.Value = newHealth;

        OnCarDamaged.Invoke(damagePosition);

        Debug.Log($"[CarDamageManager] Car took {damageDealt} damage");

        // Show damage number on UI
        DamageNumberPool.Instance.ShowDamageNumber(damagePosition, damageDealt, attackerId, victimId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ApplyDamageToPartServerRpc(CarPartType partType, float damage, Vector3 damagePosition, ulong attackerClientId)
    {
        ApplyDamageToPart(partType, damage, damagePosition, attackerClientId);
    }

    public void Repair(float amount)
    {
        // Only server can repair
        if (!IsServer)
        {
            Debug.LogWarning("[CarDamageManager] Repair called on client - use ServerRpc instead");
            return;
        }

        currentCarHealth.Value = Mathf.Min(currentCarHealth.Value + amount, maxCarHealth);

        return; // Currently not repairing individual parts
    }

    [ServerRpc(RequireOwnership = false)]
    public void RepairServerRpc(float amount)
    {
        Repair(amount);

        return; // Currently not repairing individual parts

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

    private void OnHealthChanged(float oldValue, float newValue)
    {
        // Check if car was destroyed
        if (oldValue > 0 && newValue <= 0)
        {
            OnCarDestroyed?.Invoke();
        }
    }

    private float[] DistributeValueWithClamp(float[] array, float additionValue, float maxLimit = 100f)
    {
        if (array == null || array.Length == 0)
            throw new ArgumentException("Array cannot be null or empty");

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

    private float GetDamageReductionMultiplier(CarPartType carPartHit)
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
