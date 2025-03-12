using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;

    public CarDamageManager DamageManager { get; private set; }
    public event Action OnPlayerCarDamaged; 

    private void Awake()
    {
        Instance = this;

        DamageManager = GetComponent<CarDamageManager>();
        DamageManager.OnCarDamaged += () => OnPlayerCarDamaged?.Invoke();
    }

    private void OnDisable()
    {
        DamageManager.OnCarDamaged -= () => OnPlayerCarDamaged?.Invoke();
    }
}
