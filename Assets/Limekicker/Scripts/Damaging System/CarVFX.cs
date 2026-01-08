using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

[RequireComponent(typeof(NetworkObject))]
public class CarVFX : NetworkBehaviour
{
    [Header("Damage Effects")]
    [SerializeField] private ParticleSystem[] damageSmokeVFXs;
    [SerializeField] private ParticleSystem damageImpactEffect;
    [SerializeField] private ParticleSystem fireVFX;

    [SerializeField] private Color normalDamageSmokeColor;
    [SerializeField] private Color heavyDamageSmokeColor;

    private CarDamageManager carDamageManager;

    public void Init(CarDamageManager carDamageManager)
    {
        this.carDamageManager = carDamageManager;

        carDamageManager.OnCarDamaged += HandleCarDamageVFX;
    }

    private void HandleCarDamageVFX(float damageAmount, Vector3 damagePosition)
    {
        float healthPercentage = carDamageManager.CarHealthPercentage;
        //Debug.Log($"CarVFX: Handling car damage VFX with {healthPercentage} percentage of health left on {carDamageManager.PlayerController.PlayerName}");

        // Play effects locally on server
        PlayDamageVFXLocal(damageAmount, damagePosition, healthPercentage);

        // If we're on the server, sync to all clients
        if (IsServer)
        {
            PlayDamageVFXClientRpc(damageAmount, damagePosition, healthPercentage);
        }
    }

    [ClientRpc]
    private void PlayDamageVFXClientRpc(float damageAmount, Vector3 damagePosition, float healthPercentage)
    {
        // Only play on clients (server already played locally)
        if (!IsServer)
        {
            PlayDamageVFXLocal(damageAmount, damagePosition, healthPercentage);
        }
    }

    private void PlayDamageVFXLocal(float damageAmount, Vector3 damagePosition, float healthPercentage)
    {
        if (damageImpactEffect != null)
        {
            damageImpactEffect.gameObject.SetActive(true);
            damageImpactEffect.transform.position = damagePosition;
            damageImpactEffect.Play();
        }

        Debug.Log($"CarVFX: Playing damage VFX locally with {healthPercentage} percentage of health left on {carDamageManager.PlayerController.PlayerName.Value}");

        // Check if smoke VFX array is valid
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

    public override void OnNetworkDespawn()
    {
        base.OnNetworkDespawn();

        if (carDamageManager != null)
            carDamageManager.OnCarDamaged -= HandleCarDamageVFX;
    } 
}
