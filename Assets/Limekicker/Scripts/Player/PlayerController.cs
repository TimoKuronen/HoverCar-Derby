using Cinemachine;
using System;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [SerializeField] private PlayerData playerData;
    [SerializeField] private CinemachineVirtualCamera playerCamera;
    [SerializeField] private int cameraPriority = 10;

    public CarDamageManager DamageManager { get; private set; }
    public PlayerData PlayerData => playerData;
    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>(new FixedString32Bytes("Player"));

    public static event Action<PlayerController> OnPlayerSpawned;
    public static event Action<PlayerController> OnPlayerDespawned;
    public event Action OnPlayerCarDamaged;

    private NitroBoost nitroBoost;

    public override void OnNetworkSpawn()
    {
        if (IsOwner)
        {
            nitroBoost = GetComponent<NitroBoost>();
            DamageManager = GetComponent<CarDamageManager>();

            DamageManager.OnCarDamaged += () => OnPlayerCarDamaged?.Invoke();

            playerCamera.Priority = cameraPriority;
        }

        if (IsServer)
        {
            UserData userdata = HostSingleton.Instance.GameManager.NetworkServer.GetUserData(OwnerClientId);
            PlayerName.Value = userdata.Username;

            OnPlayerSpawned?.Invoke(this);
        }
    }

    private void OnDisable()
    {
        DamageManager.OnCarDamaged -= () => OnPlayerCarDamaged?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            OnPlayerDespawned?.Invoke(this);
        }
    }
}
