using Cinemachine;
using System;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineVirtualCamera playerCamera;
    [SerializeField] private CarColorPainter carColorPainter;
    [SerializeField] private CarDamageManager damageManager;
    [SerializeField] private Rigidbody cachedRigidbody;

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

    private EventBinding<GameStateChangeEvent> gameStateChangeEvent;

    #region Network Lifecycle
    public override void OnNetworkSpawn()
    {
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
        }
        else if (IsBot)
        {
            PlayerName.Value = new FixedString32Bytes("Bot " + PlayerIndex.Value);
        }

        PlayerIndex.OnValueChanged += OnPlayerIndexChanged;

        if (PlayerIndex.Value > 0)
        {
            OnPlayerIndexChanged(0, PlayerIndex.Value);
        }

        gameStateChangeEvent = new EventBinding<GameStateChangeEvent>(HandleGameStateChange);
        EventBus<GameStateChangeEvent>.Register(gameStateChangeEvent);
    }

    public override void OnNetworkDespawn()
    {
        try
        {
            PlayerIndex.OnValueChanged -= OnPlayerIndexChanged;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[PlayerController] Failed to unsubscribe from PlayerIndex (expected during shutdown): {e.Message}");
        }

        EventBus<GameStateChangeEvent>.Unregister(gameStateChangeEvent);
    }
    #endregion

    #region Public Methods
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
    #endregion

    #region Private Methods
    private void HandleGameStateChange(GameStateChangeEvent @event)
    {
        if (@event.NewState is CountdownState)
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
            playerCamera.Priority = 0;
            playerCamera.enabled = false;
        }
    }

    /// <summary>
    /// Coroutine to teleport the player to a specific position. Called by ClientRPC.
    /// </summary>
    private IEnumerator TeleportToPositionCoroutine(Vector3 position, Quaternion rotation)
    {
        yield return new WaitForSeconds(0.05f);

        if (!IsOwner) 
            yield break;

        if (cachedRigidbody != null)
        {
            cachedRigidbody.isKinematic = true;
            cachedRigidbody.position = position;
            cachedRigidbody.rotation = rotation;
            cachedRigidbody.velocity = Vector3.zero;
            cachedRigidbody.angularVelocity = Vector3.zero;
        }

        transform.SetPositionAndRotation(position, rotation);

        if (TryGetComponent<Unity.Netcode.Components.NetworkTransform>(out var networkTransform))
        {
            networkTransform.Teleport(position, rotation, Vector3.one);
        }

        if (cachedRigidbody != null)
        {
            cachedRigidbody.isKinematic = false;
            Physics.SyncTransforms();
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
        }
    }
    #endregion

    // temporary point giver to bot 
    private void Update()
    {
        if (!IsBot)
            return;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Keyboard.current != null && Keyboard.current.uKey.wasPressedThisFrame)
        {
            EventBus<DamageDealtEvent>.Raise(new DamageDealtEvent
            {
                AttackerClientId = NetworkObjectId,
                DamageAmount = 10f
            });
        }
#endif
    }
}
