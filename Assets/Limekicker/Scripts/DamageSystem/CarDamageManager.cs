using System;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-authoritative car health, damage application, and destruction events.
/// </summary>
public class CarDamageManager : NetworkBehaviour
{
    private NetworkVariable<float> currentCarHealth = new NetworkVariable<float>(
        100f,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private readonly float maxCarHealth = 100f;
    private DamageNumberSync damageNumberSync;

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        damageNumberSync = GetComponent<DamageNumberSync>();

        if (IsServer && currentCarHealth.Value == 0)
        {
            currentCarHealth.Value = maxCarHealth;
        }

        currentCarHealth.OnValueChanged += OnHealthChanged;
    }

    public override void OnNetworkDespawn()
    {
        currentCarHealth.OnValueChanged -= OnHealthChanged;
        base.OnNetworkDespawn();
    }

    public PlayerController PlayerController { get; private set; }
    public float CarHealthPercentage => currentCarHealth.Value / maxCarHealth * 100f;
    public float CurrentCarHealth => currentCarHealth.Value;
    public bool IsDestroyed => currentCarHealth.Value <= 0f;

    public event Action OnCarDestroyed;
    public event Action<Vector3> OnCarDamaged;

    public void Initialize(PlayerController playerController)
    {
        PlayerController = playerController;
    }

    /// <summary>
    /// Applies damage to this car. Must be called on server. Updates health, triggers VFX,
    /// displays damage numbers, and raises damage events for scoring.
    /// </summary>
    public void ApplyDamage(float damage, Vector3 damagePosition, ulong? attackerClientId = null)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[CarDamageManager] ApplyDamage called on client - use ServerRpc instead");
            return;
        }

        ulong attackerId = attackerClientId ?? ulong.MaxValue;
        ulong victimId = ulong.MaxValue;

        if (PlayerController != null && NetworkObject != null)
        {
            victimId = PlayerController.IsBot ? NetworkObject.NetworkObjectId : PlayerController.OwnerClientId;
        }

        float newHealth = Mathf.Max(0f, currentCarHealth.Value - damage);
        currentCarHealth.Value = newHealth;

        OnCarDamaged.Invoke(damagePosition);
        Debug.Log($"[CarDamageManager] Car took {damage} damage");

        if (damageNumberSync != null)
        {
            damageNumberSync.ShowDamageNumberRpc(damagePosition, damage, attackerId, victimId);
        }
        else if (DamageNumberPool.Instance != null)
        {
            DamageNumberPool.Instance.ShowDamageNumber(damagePosition, damage, attackerId, victimId);
        }

        if (attackerId != ulong.MaxValue && damage > 0)
        {
            EventBus<DamageDealtEvent>.Raise(new DamageDealtEvent
            {
                AttackerClientId = attackerId,
                DamageAmount = damage
            });
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void ApplyDamageServerRpc(float damage, Vector3 damagePosition, ulong attackerClientId)
    {
        ApplyDamage(damage, damagePosition, attackerClientId);
    }

    public void Repair(float amount)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[CarDamageManager] Repair called on client - use ServerRpc instead");
            return;
        }

        currentCarHealth.Value = Mathf.Min(currentCarHealth.Value + amount, maxCarHealth);
    }

    [ServerRpc(RequireOwnership = false)]
    public void RepairServerRpc(float amount)
    {
        Repair(amount);
    }

    private void OnHealthChanged(float oldValue, float newValue)
    {
        if (oldValue > 0 && newValue <= 0)
        {
            OnCarDestroyed?.Invoke();
        }
    }
}
