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
    [SerializeField] private CarDamageManager damageManager;

    [Header("Settings")]
    [SerializeField] private int cameraPriority = 10;
    [SerializeField] private float spawnRotationDelay = 0.5f; // Delay to account for server overrides

    public CarDamageManager DamageManager => damageManager;
    public bool IsBot { get; private set; }

    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>(new FixedString32Bytes("Player"));
    public NetworkVariable<int> PlayerIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    public static event Action<PlayerController> OnPlayerSpawned;
    public static event Action<PlayerController> OnPlayerDespawned;

    private ISpawnPointService spawnPointService;

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
            // Client-side: Invoke OnPlayerSpawned for local client so camera can attach but NOT for bots
            OnPlayerSpawned?.Invoke(this);
        }
        else if (IsBot)
        {
            // Bot: Just set name, don't trigger events
            PlayerName.Value = new FixedString32Bytes("Bot " + PlayerIndex.Value);
        }

        // Subscribe to PlayerIndex changes to apply colors when it's set by server
        PlayerIndex.OnValueChanged += OnPlayerIndexChanged;

        // Apply initial value if already set (for late joiners)
        if (PlayerIndex.Value > 0)
        {
            OnPlayerIndexChanged(0, PlayerIndex.Value);
        }

        // Apply spawn point rotation after a delay (to account for server overrides)
        // Only run on server where spawn point service data exists
        if (IsServer)
        {
            StartCoroutine(ApplySpawnPointRotation());
        }

        SetPlayerCamera();
    }

    private void SetPlayerCamera()
    {
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
    }

    /// <summary>
    /// Attempts to resolve ISpawnPointService from VContainer and apply spawn point rotation.
    /// Only runs on server where spawn point service data exists.
    /// </summary>
    private IEnumerator ApplySpawnPointRotation()
    {
        // Only run on server
        if (!IsServer)
            yield break;

        // Wait for the delay to account for server overrides
        yield return new WaitForSeconds(spawnRotationDelay);

        // Try to resolve the spawn point service
        TryResolveSpawnPointService();

        if (spawnPointService != null)
        {
            var spawnData = spawnPointService.GetSpawnPointForObject(NetworkObject);
            if (spawnData != null)
            {
                // Apply the spawn point position and rotation (ensures correct spawn position after NetworkTransform sync)
                transform.SetPositionAndRotation(spawnData.Position, spawnData.Rotation);
                Debug.Log($"[PlayerController] Applied spawn point position and rotation: {spawnData.Position}, {spawnData.Rotation.eulerAngles}");
            }
            else
            {
                Debug.LogWarning($"[PlayerController] Could not find spawn point data for {gameObject.name} - player may spawn at incorrect position!");
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
