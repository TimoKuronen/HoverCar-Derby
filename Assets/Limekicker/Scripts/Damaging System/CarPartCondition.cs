using System;

using UnityEngine;

public class CarPartCondition : MonoBehaviour
{

    [SerializeField] private PartStages[] carPartStates;

    private CarPart carPart;
    private int currentConditionIndex;

    void Start()
    {
        carPart = GetComponent<CarPart>();
        carPart.OnCarPartHealthUpdated += HandlePartDamage;

        CarPartConditionEffects[] effects = GetComponentsInChildren<CarPartConditionEffects>();

        for (int i = 0; i < carPartStates.Length; i++)
        {
            foreach (var item in effects)
            {
                if (carPartStates[i].carPartState == item.PartCondition)
                    carPartStates[i].stateObject = item;
            }
        }
    }

    private void HandlePartDamage(float currentHealth)
    {
        float[] stagesArray = new float[carPartStates.Length];
        for (int i = 0; i < carPartStates.Length; i++)
        {
            stagesArray[i] = carPartStates[i].stateHealth;
        }

        int stateIndex = MathMethods.GetClosestIndex(stagesArray, currentHealth);
        if (stateIndex != currentConditionIndex)
        {
            UpdateCondition(stateIndex);
        }
    }

    private void UpdateCondition(int newIndex)
    {
        currentConditionIndex = newIndex;
        for (int i = 0; i < carPartStates.Length; i++)
        {
            if (i == currentConditionIndex)
            {
                carPartStates[i].stateObject.Toggle(true);
            }
            else carPartStates[i].stateObject.Toggle(false);
        }
    }
}

[Serializable]
public struct PartStages
{
    public CarPartState carPartState;
    public float stateHealth;
    public CarPartConditionEffects stateObject;
}

public enum CarPartState
{
    Intact,
    LightDamage,
    ModerateDamage,
    CriticalDamage,
    Destroyed
}