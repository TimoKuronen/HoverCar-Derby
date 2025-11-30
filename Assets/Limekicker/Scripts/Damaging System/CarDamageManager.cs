using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class CarDamageManager : MonoBehaviour
{
    [Header("Damage Effects")]
    [SerializeField] private ParticleSystem damageSmoke;
    [SerializeField] private ParticleSystem damageImpactEffect;
    [SerializeField] private Color normalDamageSmokeColor;
    [SerializeField] private Color heavyDamageSmokeColor;

    private CarManager carManager;
    private float totalHealth;
    private ParticleSystem.EmissionModule emissionModule;
    private NetworkObject networkObject;
    private PlayerController playerController;

    public Dictionary<CarPartType, CarPart> CarParts { get; private set; } = new Dictionary<CarPartType, CarPart>();

    public event Action OnCarDestroyed;
    public event Action OnCarDamaged; // Legacy event (no parameters)
    public event Action<float, Vector3> OnCarDamagedWithDetails; // New event with damage amount and position

    private void Start()
    {
        carManager = GetComponent<CarManager>();
        networkObject = GetComponent<NetworkObject>();
        playerController = GetComponent<PlayerController>();
        
        //CarParts.Add(CarPartType.FrontBumper, GetComponentInChildren<FrontBumper>());
        //CarParts.Add(CarPartType.SidePanel_Left, transform.Find("CarPart_SidePanel_Left").GetComponent<SidePanel>());
        //CarParts.Add(CarPartType.SidePanel_Right, transform.Find("CarPart_SidePanel_Right").GetComponent<SidePanel>());
        //CarParts.Add(CarPartType.RearBumper, GetComponentInChildren<RearBumper>());

        emissionModule = damageSmoke.emission;

        foreach (var part in CarParts)
        {
            part.Value.SetMaxHealth(carManager.CarData);
            totalHealth += part.Value.CurrentHealth.Value;
        }
    }

    private void Update()
    {
#if UNITY_EDITOR
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            CarPartType[] parts = (CarPartType[])Enum.GetValues(typeof(CarPartType));
            int index = UnityEngine.Random.Range(0, parts.Length);

            ApplyDamageToPart(parts[index], 33);
        }

        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            Repair(30);
        }
#endif
    }

    public void ApplyDamageToPart(CarPartType partType, float damage, Vector3? damagePosition = null, ulong? attackerClientId = null)
    {
        if (CarParts.TryGetValue(partType, out CarPart part) && part != null)
        {
            float damageDealt = damage * GetDamageReductionMultiplier(partType);
            part.TakeDamage(damageDealt);
            totalHealth -= damageDealt;

            // Get damage position (use part transform if not provided)
            Vector3 finalDamagePosition = damagePosition ?? (part.transform != null ? part.transform.position : transform.position);
            
            // Get attacker and victim client IDs
            ulong attackerId = attackerClientId ?? ulong.MaxValue;
            ulong victimId = ulong.MaxValue;
            if (playerController != null && networkObject != null)
            {
                victimId = playerController.OwnerClientId;
            }

            // Show damage number if damage is significant
            if (damageDealt > 0.1f)
            {
                ShowDamageNumber(finalDamagePosition, damageDealt, attackerId, victimId);
            }

            // Invoke both events for backward compatibility
            OnCarDamaged?.Invoke();
            OnCarDamagedWithDetails?.Invoke(damageDealt, finalDamagePosition);
        }

        if (totalHealth <= 0)
        {
            OnCarDestroyed?.Invoke();
        }
    }

    /// <summary>
    /// Shows a damage number for this car. Works with both networked and non-networked scenarios.
    /// </summary>
    private void ShowDamageNumber(Vector3 worldPosition, float damageAmount, ulong attackerClientId, ulong victimClientId)
    {
        // Try to use DamageNumberSync component first (for networked scenarios)
        DamageNumberSync damageSync = GetComponent<DamageNumberSync>();
        if (damageSync != null && NetworkManager.Singleton != null && NetworkManager.Singleton.IsServer)
        {
            damageSync.ShowDamageNumberRpc(worldPosition, damageAmount, attackerClientId, victimClientId);
        }
        else
        {
            // Fallback: use DamageNumberPool directly (for non-networked or client-side scenarios)
            DamageNumberPool pool = DamageNumberPool.Instance;
            if (pool != null)
            {
                pool.ShowDamageNumber(worldPosition, damageAmount, attackerClientId, victimClientId);
            }
        }
    }

    public void DealDamageByCar(Collision collision)
    {

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
