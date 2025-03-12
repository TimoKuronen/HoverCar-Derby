using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class CarDamageManager : MonoBehaviour
{
    public event Action OnCarDamaged;
    public Dictionary<CarPartType, CarPart> CarParts { get; private set; } = new Dictionary<CarPartType, CarPart>();

    private void Start()
    {
        CarParts[CarPartType.FrontBumper] = GetComponentInChildren<FrontBumper>();
        CarParts[CarPartType.SidePanel_Left] = transform.Find("CarPart_SidePanel_Left").GetComponent<SidePanel>();
        CarParts[CarPartType.SidePanel_Right] = transform.Find("CarPart_SidePanel_Right").GetComponent<SidePanel>();
        CarParts[CarPartType.RearBumper] = GetComponentInChildren<RearBumper>();
    }

    public void ApplyDamageToPart(CarPartType partType, float damage)
    {
        OnCarDamaged?.Invoke();

        if (CarParts.TryGetValue(partType, out CarPart part) && part != null)
        {
            part.TakeDamage(damage);
        }
    }

    public void Repair(float amount)
    {

    }

    private float[] DistributeValue(float[] array, float additionValue)
    {
        if (array == null || array.Length == 0)
            throw new System.ArgumentException("Array cannot be null or empty");

        float minValue = Mathf.Min(array);
        float maxValue = Mathf.Max(array);

        // Prevent division by zero if all elements are the same
        if (minValue == maxValue)
            return array.Select(val => val + (additionValue / array.Length)).ToArray();

        float totalWeight = 0;
        float[] weights = new float[array.Length];

        // Calculate weights inversely proportional to magnitude
        for (int i = 0; i < array.Length; i++)
        {
            weights[i] = maxValue - array[i]; // Lower values get higher weight
            totalWeight += weights[i];
        }

        float[] result = new float[array.Length];

        // Distribute additionValue based on weights
        for (int i = 0; i < array.Length; i++)
        {
            float distributedValue = (weights[i] / totalWeight) * additionValue;
            result[i] = array[i] + distributedValue;
        }

        return result;
    }
}
