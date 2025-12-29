using Cinemachine;
using System;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using VContainer;

public class PlayerController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineVirtualCamera playerCamera;
    [SerializeField] private CarColorPainter carColorPainter;
    [SerializeField] private CarDamageManager DamageManager;

    [Header("Settings")]
    [SerializeField] private int cameraPriority = 10;
    [SerializeField] private float spawnRotationDelay = 0.5f; // Delay to account for server overrides

    public bool IsBot { get; private set; }

    private ISpawnPointService spawnPointService;

    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>(new FixedString32Bytes("Player"));
    public NetworkVariable<int> PlayerIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public static event Action<PlayerController> OnPlayerSpawned;
    public static event Action<PlayerController> OnPlayerDespawned;
    public event Action OnPlayerCarDamaged;

    public override void OnNetworkSpawn()
    {
        // Check if this is a bot - bots should not trigger player spawn events
        IsBot = GetComponent<BotPlayerController>() != null;

        if (IsServer && !IsBot)
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

            if (userdata != null)
            {
                PlayerName.Value = userdata.userName;
            }

            OnPlayerSpawned?.Invoke(this);
        }
        else if (IsOwner && !IsBot)
        {
            // Client-side: Invoke OnPlayerSpawned for local client so camera can attach
            // But NOT for bots
            OnPlayerSpawned?.Invoke(this);
            Debug.Log($"[PlayerController] Client-side OnPlayerSpawned fired for local player (ClientId: {OwnerClientId})");
        }
        else if (IsBot)
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

        // Subscribe to PlayerIndex changes to apply colors when it's set by server
        PlayerIndex.OnValueChanged += OnPlayerIndexChanged;

        // Apply initial value if already set (for late joiners)
        if (PlayerIndex.Value > 0)
        {
            OnPlayerIndexChanged(0, PlayerIndex.Value);
        }

        // Initialize damage manager subscription (this is player-specific, not index-specific)
        if (DamageManager != null)
        {
            DamageManager.OnCarDamaged += () => OnPlayerCarDamaged?.Invoke();
        }

        // Apply spawn point rotation after a delay (to account for server overrides)
        StartCoroutine(ApplySpawnPointRotation());
    }

    /// <summary>
    /// Attempts to resolve ISpawnPointService from VContainer and apply spawn point rotation.
    /// </summary>
    private IEnumerator ApplySpawnPointRotation()
    {
        // Wait for the delay to account for server overrides
        yield return new WaitForSeconds(spawnRotationDelay);

        // Try to resolve the spawn point service
        TryResolveSpawnPointService();

        if (spawnPointService != null)
        {
            var spawnData = spawnPointService.GetSpawnPointForObject(NetworkObject);
            if (spawnData != null)
            {
                // Apply the spawn point rotation
                transform.SetPositionAndRotation(spawnData.Position, spawnData.Rotation);
                Debug.Log($"[PlayerController] Applied spawn point rotation: {spawnData.Rotation.eulerAngles}");
            }
            else
            {
                Debug.LogWarning($"[PlayerController] Could not find spawn point data for {gameObject.name}");
            }
        }
    }

    /// <summary>Attempts to resolve ISpawnPointService from VContainer.</summary>
    private void TryResolveSpawnPointService()
    {
        // Try to find GameLifetimeScope which has ISpawnPointService registered
        var gameScope = FindFirstObjectByType<GameLifetimeScope>();
        if (gameScope != null)
        {
            try
            {
                var container = gameScope.Container;
                spawnPointService = container.Resolve<ISpawnPointService>();
                Debug.Log("[PlayerController] Successfully resolved ISpawnPointService from container.");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PlayerController] Failed to resolve ISpawnPointService from container: {ex.Message}");
            }
        }
        else
        {
            Debug.LogWarning("[PlayerController] GameLifetimeScope not found. Cannot resolve spawn point service.");
        }
    }

    /// <summary>
    /// Called when PlayerIndex NetworkVariable changes (set by server via Initialize method).
    /// Applies car color based on the assigned index.
    /// </summary>
    private void OnPlayerIndexChanged(int oldIndex, int newIndex)
    {
        if (carColorPainter != null && newIndex >= 0)
        {
            carColorPainter.AssignColor(newIndex);
            Debug.Log($"[PlayerController] PlayerIndex changed to {newIndex}, applied car color");
        }
    }

    /// <summary>
    /// Called by PlayerSpawnManager on server to set the player's index.
    /// This is the single source of truth - server sets it, clients receive it via NetworkVariable.
    /// </summary>
    public void Initialize(int playerIndex)
    {
        if (!IsServer)
        {
            Debug.LogWarning("[PlayerController] Initialize called on client - this should only be called on server!");
            return;
        }

        PlayerIndex.Value = playerIndex;
        Debug.Log($"[PlayerController] Server initialized player with index: {playerIndex}");
    }

    private void OnDisable()
    {
        //DamageManager.OnCarDamaged -= () => OnPlayerCarDamaged?.Invoke();
    }

    public override void OnNetworkDespawn()
    {
        // Unsubscribe from NetworkVariable events
        try
        {
            PlayerIndex.OnValueChanged -= OnPlayerIndexChanged;
        }
        catch (System.Exception e)
        {
            // NetworkVariable might be destroyed during shutdown - this is expected
            Debug.LogWarning($"[PlayerController] Failed to unsubscribe from PlayerIndex (expected during shutdown): {e.Message}");
        }

        // Invoke despawn event if we're server and there are subscribers
        if (IsServer && OnPlayerDespawned != null)
        {
            try
            {
                OnPlayerDespawned.Invoke(this);
            }
            catch (System.Exception e)
            {
                // Subscribers might be destroyed during shutdown - this is expected
                Debug.LogWarning($"[PlayerController] Exception during OnPlayerDespawned (expected during shutdown): {e.Message}");
            }
        }
    }
}
