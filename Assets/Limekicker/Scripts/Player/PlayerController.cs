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

    private EventBinding<GameStateChangeEvent> gameStateChangeEvent;

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

        gameStateChangeEvent = new EventBinding<GameStateChangeEvent>(HandleGameStateChange);
        EventBus<GameStateChangeEvent>.Register(gameStateChangeEvent);
    }

    private void HandleGameStateChange(GameStateChangeEvent @event)
    {
        switch (@event.NewState)
        {
            case CountdownState:
                SetPlayerCamera();
                break;
            default:              
                break;
        }
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
    /// ClientRPC called by PlayerSpawnManager to teleport the player to spawn position.
    /// This is needed for client-authoritative transforms where the client must set its own position.
    /// PlayerSpawnManager maintains responsibility for spawn logic; this is just a simple teleport interface.
    /// </summary>
    [ClientRpc]
    public void TeleportToSpawnPositionClientRpc(Vector3 position, Quaternion rotation)
    {
        if (!IsOwner)
            return;

        StartCoroutine(TeleportToPositionCoroutine(position, rotation));
    }

    /// <summary>
    /// Coroutine to teleport the player to a specific position. Called by ClientRPC.
    /// </summary>
    private IEnumerator TeleportToPositionCoroutine(Vector3 position, Quaternion rotation)
    {
        // 1. Give the client a moment to finish its internal 'Spawn' handshake
        // If you teleport too fast, the NetworkTransform might not be ready to 'Commit'
        yield return new WaitForSeconds(0.05f);

        if (!IsOwner) 
            yield break;

        // 2. Handle Rigidbody if it exists (Crucial for cars)
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; // Temporarily stop physics interference
            rb.position = position;
            rb.rotation = rotation;
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        // 3. Apply to Transform
        transform.SetPositionAndRotation(position, rotation);

        // 4. Force the NetworkTransform to recognize the 'Teleport'
        if (TryGetComponent<Unity.Netcode.Components.NetworkTransform>(out var networkTransform))
        {
            networkTransform.Teleport(position, rotation, Vector3.one);
        }

        // 5. Re-enable physics
        if (rb != null)
        {
            rb.isKinematic = false;
            // Force physics engine to sync with the new transform position immediately
            Physics.SyncTransforms();
        }

        Debug.Log($"[PlayerController] Client successfully teleported and synced to: {position}");
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

        EventBus<GameStateChangeEvent>.Unregister(gameStateChangeEvent);
    }
}
