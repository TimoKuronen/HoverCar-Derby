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
    public int Cash { get; private set; } // sync this with leaderbaord
    public PlayerData PlayerData => playerData;
    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>(new FixedString32Bytes("Player"));

    public static event Action<PlayerController> OnPlayerSpawned;
    public static event Action<PlayerController> OnPlayerDespawned;
    public event Action OnPlayerCarDamaged;

    private NitroBoost nitroBoost;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            UserData userdata = null;

            if (IsHost)
            {
                userdata = HostSingleton.Instance.GameManager.NetworkServer.GetUserData(OwnerClientId);
            }
            else
            {
                userdata = ServerSingleton.Instance.GameManager.NetworkServer.GetUserData(OwnerClientId);
            }

            Debug.Log($"Player spawned: {userdata}");

            PlayerName.Value = userdata.userName;

            OnPlayerSpawned?.Invoke(this);
        }

        if (IsOwner)
        {
            nitroBoost = GetComponent<NitroBoost>();
            DamageManager = GetComponent<CarDamageManager>();

            DamageManager.OnCarDamaged += () => OnPlayerCarDamaged?.Invoke();

            if (playerCamera != null)
            {
                playerCamera.Priority = cameraPriority;
                playerCamera.enabled = true;
            }
        }
        else if (playerCamera != null)
        {
            // Lower priority and disable non-owner cameras so host doesn't switch to joining client's camera
            playerCamera.Priority = 0;
            playerCamera.enabled = false;
        }
    }

    private void OnDisable()
    {
        //DamageManager.OnCarDamaged -= () => OnPlayerCarDamaged?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        if (IsServer)
        {
            OnPlayerDespawned?.Invoke(this);
        }
    }
}
