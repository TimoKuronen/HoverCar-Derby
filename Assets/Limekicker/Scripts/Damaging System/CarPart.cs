using System;
using Unity.Netcode;
using UnityEngine;
public abstract class CarPart : NetworkBehaviour
{
    [SerializeField] private float maxHealth = 100f;

    public NetworkVariable<float> CurrentHealth = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public event Action<float> OnCarPartHealthUpdated;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        
        // Subscribe to health changes
        CurrentHealth.OnValueChanged += OnHealthChanged;
        
        // Initialize health on server
        if (IsServer && CurrentHealth.Value == 0)
        {
            CurrentHealth.Value = maxHealth;
        }
        
        // Invoke initial value
        OnCarPartHealthUpdated?.Invoke(CurrentHealth.Value);
    }

    public override void OnNetworkDespawn()
    {
        CurrentHealth.OnValueChanged -= OnHealthChanged;
        base.OnNetworkDespawn();
    }

    private void OnHealthChanged(float oldValue, float newValue)
    {
        OnCarPartHealthUpdated?.Invoke(newValue);
        
        if (newValue <= 0 && oldValue > 0)
        {
            OnDestroyed();
        }
    }

    public void SetMaxHealth(CarData carData)
    {
        maxHealth *= carData.GetEnduranceMultiplier();
    }

    public virtual void TakeDamage(float damage)
    {
        // Only server can apply damage
        if (!IsServer)
        {
            Debug.LogWarning("[CarPart] TakeDamage called on client - use ServerRpc instead");
            return;
        }

        float newHealth = Mathf.Max(0f, CurrentHealth.Value - damage);
        CurrentHealth.Value = newHealth;
    }

    [ServerRpc(RequireOwnership = false)]
    public void TakeDamageServerRpc(float damage)
    {
        TakeDamage(damage);
    }

    public virtual void RepairPart(float newHealthAmount)
    {
        // Only server can repair
        if (!IsServer)
        {
            Debug.LogWarning("[CarPart] RepairPart called on client - use ServerRpc instead");
            return;
        }

        Debug.Log("repairing from " + CurrentHealth.Value + " to " + newHealthAmount);
        CurrentHealth.Value = Mathf.Clamp(newHealthAmount, 0f, maxHealth);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RepairPartServerRpc(float newHealthAmount)
    {
        RepairPart(newHealthAmount);
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