using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;

    public CarDamageManager DamageManager { get; private set; }
    public event Action OnPlayerCarDamaged;
    private NitroBoost nitroBoost;

    private void Awake()
    {
        Instance = this;

        nitroBoost = GetComponent<NitroBoost>();
        DamageManager = GetComponent<CarDamageManager>();
        DamageManager.OnCarDamaged += () => OnPlayerCarDamaged?.Invoke();
    }

    private void OnDisable()
    {
        DamageManager.OnCarDamaged -= () => OnPlayerCarDamaged?.Invoke();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.LeftShift))
        {
            if (nitroBoost.CanUse())
                nitroBoost.ToggleNitro();
        }
        if (Input.GetKeyUp(KeyCode.LeftShift))
        {
            nitroBoost.ToggleNitro();
        }
    }
}
