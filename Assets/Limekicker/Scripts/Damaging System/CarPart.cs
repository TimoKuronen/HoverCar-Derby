using System;
using UnityEngine;
public abstract class CarPart : MonoBehaviour
{
    public float maxHealth = 100f;
    public float CurrentHealth { get; private set; }
    public event Action<float> OnCarPartHealthUpdated;

    protected virtual void Start()
    {
        CurrentHealth = maxHealth;

        CurrentHealth = UnityEngine.Random.Range(30, maxHealth);

        OnCarPartHealthUpdated?.Invoke(CurrentHealth);
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