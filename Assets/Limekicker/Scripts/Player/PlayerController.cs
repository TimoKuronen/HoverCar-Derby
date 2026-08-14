using Cinemachine;
using System;
using System.Collections;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Networked player identity with camera, score, and spawn lifecycle.
/// </summary>
public class PlayerController : NetworkBehaviour
{
    [Header("References")]
    [SerializeField] private CinemachineVirtualCamera playerCamera;
    [SerializeField] private CarColorPainter carColorPainter;
    [SerializeField] private CarDamageManager damageManager;
    [SerializeField] private Rigidbody cachedRigidbody;

    [Header("Settings")]
    [SerializeField] private int cameraPriority = 10;

    private EventBinding<GameStateChangeEvent> gameStateChangeEvent;

    public CarDamageManager DamageManager => damageManager;
    public bool IsBot { get; private set; }

    public NetworkVariable<FixedString32Bytes> PlayerName = new NetworkVariable<FixedString32Bytes>(new FixedString32Bytes("Player"));
    public NetworkVariable<int> PlayerIndex = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );
    public NetworkVariable<int> Score = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

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

        if (IsOwner && !IsBot)
        {
            StartCoroutine(ReportReadyWhenInitialized());
            StartCoroutine(SyncCameraAfterSpawn());
        }

        StartCoroutine(RaisePlayerSpawnedEventNextFrame());
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

        if (IsServer)
        {
            var gameManager = FindFirstObjectByType<GameManager>();
            gameManager?.UnregisterParticipant(NetworkObjectId);
        }

        var trackerGameManager = FindFirstObjectByType<GameManager>();
        trackerGameManager?.PlayerTracker?.RemovePlayer(NetworkObjectId);

        EventBus<GameStateChangeEvent>.Unregister(gameStateChangeEvent);
    }

    /// <summary>
    /// Server assigns player index; replicated to clients via NetworkVariable.
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
    /// Owner client teleports after spawn; required for client-authoritative NetworkTransform.
    /// </summary>
    [ClientRpc]
    public void TeleportToSpawnPositionClientRpc(Vector3 position, Quaternion rotation)
    {
        if (!IsOwner)
            return;

        StartCoroutine(TeleportToPositionCoroutine(position, rotation));
    }

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

    private IEnumerator RaisePlayerSpawnedEventNextFrame()
    {
        yield return null;

        if (!IsSpawned)
            yield break;

        EventBus<PlayerSpawnedEvent>.Raise(new PlayerSpawnedEvent
        {
            UserData = null,
            NetworkObject = NetworkObject
        });
    }

    private IEnumerator ReportReadyWhenInitialized()
    {
        yield return null;

        if (!IsOwner || IsBot)
            yield break;

        var gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
            gameManager.ReportPlayerReadyServerRpc(NetworkObjectId);
    }

    private void HandleGameStateChange(GameStateChangeEvent @event)
    {
        if (@event.NewState is CountdownState)
            SetPlayerCamera();
    }

    private IEnumerator SyncCameraAfterSpawn()
    {
        yield return null;

        if (!IsOwner || IsBot)
            yield break;

        var manager = FindFirstObjectByType<GameManager>();
        if (manager == null)
            yield break;

        if (manager.CurrentMatchPhase is RaceContext.MatchPhase.Countdown
            or RaceContext.MatchPhase.Playing
            or RaceContext.MatchPhase.Completed)
        {
            SetPlayerCamera();
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
            playerCamera.Priority = 0;
            playerCamera.enabled = false;
        }
    }

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

    private void OnPlayerIndexChanged(int oldIndex, int newIndex)
    {
        if (carColorPainter != null && newIndex >= 0)
        {
            carColorPainter.AssignColor(newIndex);
        }
    }
}
