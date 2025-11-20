using Cinemachine;
using System;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class PlayerController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineVirtualCamera playerCamera;
    [SerializeField] private CarColorPainter carColorPainter;
    [SerializeField] private CarDamageManager DamageManager;

    [Header("Settings")]
    [SerializeField] private int cameraPriority = 10;
        
    public int Cash { get; private set; } // sync this with leaderbaord
    public int PlayerIndex { get; private set; }
    public PlayerData PlayerData { get; private set; }

    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>(new FixedString32Bytes("Player"));

    public static event Action<PlayerController> OnPlayerSpawned;
    public static event Action<PlayerController> OnPlayerDespawned;
    public event Action OnPlayerCarDamaged;

    public override void OnNetworkSpawn()
    {
        // Check if this is a bot - bots should not trigger player spawn events
        bool isBot = GetComponent<BotPlayerController>() != null;
        
        if (IsServer && !isBot)
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

            if (userdata != null)
            {
                PlayerName.Value = userdata.userName;
            }

            OnPlayerSpawned?.Invoke(this);
        }
        else if (IsOwner && !isBot)
        {
            // Client-side: Invoke OnPlayerSpawned for local client so camera can attach
            // But NOT for bots
            OnPlayerSpawned?.Invoke(this);
            Debug.Log($"[PlayerController] Client-side OnPlayerSpawned fired for local player (ClientId: {OwnerClientId})");
        }
        else if (isBot)
        {
            // Bot: Just set name, don't trigger events
            Debug.Log($"[PlayerController] Bot spawned (will not trigger camera/control events)");
        }

        if (IsOwner)
        {
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

        // Initialize must be called on all clients for car colors/styles to work
        // Server calls Initialize with proper playerIndex, but clients need it too
        if (IsOwner && !IsServer)
        {
            // On clients, calculate player index to match server's logic
            // Server uses: ConnectedClients.Count - 1 (when spawning)
            // We'll use the same logic but may need to wait a frame for count to update
            StartCoroutine(InitializeClientCoroutine());
        }
    }

    private IEnumerator InitializeClientCoroutine()
    {
        // Wait a frame to ensure ConnectedClientsIds is populated
        yield return null;
        
        if (PlayerIndex != 0) // Already initialized
            yield break;
            
        int clientIndex = 0;
        if (NetworkManager.Singleton != null && NetworkManager.Singleton.ConnectedClientsIds != null)
        {
            // On clients, we can use ConnectedClientsIds which is available on all clients
            // Match server's calculation by finding our position in the list
            var clientIds = new System.Collections.Generic.List<ulong>(NetworkManager.Singleton.ConnectedClientsIds);
            clientIndex = clientIds.IndexOf(OwnerClientId);
            
            // If we can't find ourselves, use count as fallback (server's approach)
            if (clientIndex < 0)
            {
                clientIndex = clientIds.Count - 1;
            }
        }
        else
        {
            // Fallback: use a default index (shouldn't happen but just in case)
            clientIndex = 0;
        }
        
        Initialize(clientIndex);
        Debug.Log($"[PlayerController] Client-side Initialize called with playerIndex: {clientIndex}");
    }

    public void Initialize(int playerIndex)
    {
        DamageManager.OnCarDamaged += () => OnPlayerCarDamaged?.Invoke();

        PlayerIndex = playerIndex;
        carColorPainter.AssignColor(playerIndex);
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
