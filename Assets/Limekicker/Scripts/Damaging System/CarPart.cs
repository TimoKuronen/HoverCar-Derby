using System;
using UnityEngine;
public abstract class CarPart : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    public float CurrentHealth { get; private set; }
    public event Action<float> OnCarPartHealthUpdated;

    protected virtual void Start()
    {
        OnCarPartHealthUpdated?.Invoke(CurrentHealth);
    }

    public void SetMaxHealth(CarData carData)
    {
        maxHealth *= carData.GetEnduranceMultiplier();
    }

    public virtual void TakeDamage(float damage)
    {
        CurrentHealth -= damage;
        if (CurrentHealth <= 0)
        {
            CurrentHealth = 0;
            OnDestroyed();
        }

        OnCarPartHealthUpdated?.Invoke(CurrentHealth);
    }

    public virtual void RepairPart(float newHealthAmount)
    {
        Debug.Log("repairing from " + CurrentHealth + " to " + newHealthAmount);
        CurrentHealth = newHealthAmount;

        OnCarPartHealthUpdated?.Invoke(CurrentHealth);
    }

    protected virtual void OnDestroyed()
    {
        Debug.Log($"{name} is destroyed!");
    }

    public float GetHealthPercentage()
    {
        return CurrentHealth / maxHealth;
    }
}
public enum CarPartType
{
    FrontBumper,
    SidePanel_Left,
    SidePanel_Right,
    RearBumper,
    Hull
}