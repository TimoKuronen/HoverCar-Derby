using Unity.Netcode;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class CarVFX : NetworkBehaviour
{
    #region Fields
    [Header("Damage Effects")]
    [SerializeField] private ParticleSystem[] damageSmokeVFXs;
    [SerializeField] private ParticleSystem damageImpactEffect;
    [SerializeField] private ParticleSystem fireVFX;

    [SerializeField] private Color normalDamageSmokeColor;
    [SerializeField] private Color heavyDamageSmokeColor;

    private CarDamageManager carDamageManager;
    private EventBinding<PlayerSpawnedEvent> playerSpawnedEvent;
    #endregion

    #region Public Methods
    public void Initialize(CarDamageManager carDamageManager)
    {
        this.carDamageManager = carDamageManager;
        carDamageManager.OnCarDamaged += HandleCarDamageVFX;

        playerSpawnedEvent = new EventBinding<PlayerSpawnedEvent>(ResetVFX);
        EventBus<PlayerSpawnedEvent>.Register(playerSpawnedEvent);
    }

    public void ResetVFX(PlayerSpawnedEvent playerSpawnedEvent)
    {
        if (playerSpawnedEvent.NetworkObject == null)
            return;

        if (carDamageManager == null || carDamageManager.PlayerController == null || carDamageManager.PlayerController.NetworkObject == null)
            return;

        if (playerSpawnedEvent.NetworkObject != carDamageManager.PlayerController.NetworkObject)
            return;

        StopFireEffect();
        if (damageSmokeVFXs != null)
        {
            foreach (var smokeVFX in damageSmokeVFXs)
            {
                if (smokeVFX != null)
                {
                    smokeVFX.Stop();
                }
            }
        }
    }

    public void EnableFireEffect()
    {
        if (fireVFX != null)
        {
            fireVFX.gameObject.SetActive(true);
            fireVFX.Play();
            
            if (IsServer)
            {
                EnableFireEffectClientRpc();
            }
        }
    }

    public void StopFireEffect()
    {
        if (fireVFX != null)
        {
            fireVFX.Stop();
            fireVFX.gameObject.SetActive(false);
            
            if (IsServer)
            {
                StopFireEffectClientRpc();
            }
        }
    }
    #endregion

    #region Network Lifecycle
    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (carDamageManager != null)
            carDamageManager.OnCarDamaged -= HandleCarDamageVFX;

        EventBus<PlayerSpawnedEvent>.Unregister(playerSpawnedEvent);
    }
    #endregion

    #region Private Methods
    private void HandleCarDamageVFX(Vector3 damagePosition)
    {
        float healthPercentage = carDamageManager.CarHealthPercentage;
        PlayDamageVFXLocal(damagePosition, healthPercentage);

        if (IsServer)
        {
            PlayDamageVFXClientRpc(damagePosition, healthPercentage);
        }
    }

    [ClientRpc]
    private void PlayDamageVFXClientRpc(Vector3 damagePosition, float healthPercentage)
    {
        if (!IsServer)
        {
            PlayDamageVFXLocal(damagePosition, healthPercentage);
        }
    }

    [ClientRpc]
    private void EnableFireEffectClientRpc()
    {
        if (!IsServer && fireVFX != null)
        {
            fireVFX.gameObject.SetActive(true);
            fireVFX.Play();
        }
    }

    [ClientRpc]
    private void StopFireEffectClientRpc()
    {
        if (!IsServer && fireVFX != null)
        {
            fireVFX.Stop();
            fireVFX.gameObject.SetActive(false);
        }
    }

    private void PlayDamageVFXLocal(Vector3 damagePosition, float healthPercentage)
    {
        if (damageImpactEffect != null)
        {
            damageImpactEffect.gameObject.SetActive(true);
            damageImpactEffect.transform.position = damagePosition;
            damageImpactEffect.Play();
        }

        if (damageSmokeVFXs == null || damageSmokeVFXs.Length < 3)
        {
            Debug.LogWarning($"[CarVFX] damageSmokeVFXs array is null or has insufficient elements ({damageSmokeVFXs?.Length ?? 0})");
            return;
        }

        switch (healthPercentage)
        {
            case < 20f:
                if (damageSmokeVFXs[0] != null)
                {
                    var mainModuleHigh = damageSmokeVFXs[0].main;
                    mainModuleHigh.startColor = heavyDamageSmokeColor;
                    damageSmokeVFXs[0].Play();
                }
                if (fireVFX != null)
                {
                    fireVFX.gameObject.SetActive(true);
                    fireVFX.Play();
                }
                break;
            case < 50f:
                if (damageSmokeVFXs[1] != null)
                {
                    var mainModuleMedium = damageSmokeVFXs[1].main;
                    mainModuleMedium.startColor = Color.Lerp(normalDamageSmokeColor, heavyDamageSmokeColor, 0.5f);
                    damageSmokeVFXs[1].Play();
                }
                break;
            case < 80f:
                if (damageSmokeVFXs[2] != null)
                {
                    var mainModuleLow = damageSmokeVFXs[2].main;
                    mainModuleLow.startColor = normalDamageSmokeColor;
                    damageSmokeVFXs[2].Play();
                }
                break;
        }
    }
    #endregion
}
