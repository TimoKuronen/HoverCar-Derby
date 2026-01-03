using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CarVFX : MonoBehaviour
{
    [Header("Damage Effects")]
    [SerializeField] private ParticleSystem[] damageSmokeVFXs;
    [SerializeField] private ParticleSystem damageImpactEffect;
    [SerializeField] private GameObject fireVFX;

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
        Debug.Log($"CarVFX: Handling car damage VFX with {carDamageManager.CarHealthPercentage} percentage of health left on {carDamageManager.PlayerController.PlayerName}");

        damageImpactEffect.transform.position = damagePosition;
        damageImpactEffect.Play();

        switch (carDamageManager.CarHealthPercentage)
        {
            case < 20f:
                var mainModuleHigh = damageSmokeVFXs[0].main;
                mainModuleHigh.startColor = heavyDamageSmokeColor;
                damageSmokeVFXs[0].Play();
                fireVFX.SetActive(true);
                break;
            case < 50f:
                var mainModuleMedium = damageSmokeVFXs[1].main;
                mainModuleMedium.startColor = Color.Lerp(normalDamageSmokeColor, heavyDamageSmokeColor, 0.5f);
                damageSmokeVFXs[1].Play();
                break;
            case < 80f:
                var mainModuleLow = damageSmokeVFXs[2].main;
                mainModuleLow.startColor = normalDamageSmokeColor;
                damageSmokeVFXs[2].Play();
                break;
        }
    }

    private void OnDestroy()
    {
        if (carDamageManager != null)
            carDamageManager.OnCarDamaged += HandleCarDamageVFX;
    }
}
