using System;
using Unity.Netcode;
using UnityEngine;
public abstract class CarPart : MonoBehaviour
{
    [SerializeField] private float maxHealth = 100f;
    public NetworkVariable<float> CurrentHealth = new NetworkVariable<float>();
    public event Action<float> OnCarPartHealthUpdated;

    protected virtual void Start()
    {
        OnCarPartHealthUpdated?.Invoke(CurrentHealth.Value);
    }

    public void SetMaxHealth(CarData carData)
    {
        maxHealth *= carData.GetEnduranceMultiplier();
    }

    public virtual void TakeDamage(float damage)
    {
        CurrentHealth.Value -= damage;
        if (CurrentHealth.Value <= 0)
        {
            CurrentHealth.Value = 0;
            OnDestroyed();
        }

        OnCarPartHealthUpdated?.Invoke(CurrentHealth.Value);
    }

    public virtual void RepairPart(float newHealthAmount)
    {
        Debug.Log("repairing from " + CurrentHealth + " to " + newHealthAmount);
        CurrentHealth.Value = newHealthAmount;

        OnCarPartHealthUpdated?.Invoke(CurrentHealth.Value);
    }

    protected virtual void OnDestroyed()
    {
        Debug.Log($"{name} is destroyed!");
    }

    public float GetHealthPercentage()
    {
        return CurrentHealth.Value / maxHealth;
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