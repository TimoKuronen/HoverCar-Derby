using System;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public static PlayerController Instance;

    [SerializeField] private PlayerData playerData;

    public CarDamageManager DamageManager { get; private set; }
    public PlayerData PlayerData => playerData;
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
}
