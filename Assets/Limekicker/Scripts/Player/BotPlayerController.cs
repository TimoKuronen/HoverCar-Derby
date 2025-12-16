using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Bot player controller that manages AI-controlled players for testing.
/// Handles bot initialization and provides access to bot components.
/// </summary>
[RequireComponent(typeof(HoverCarMover))]
public class BotPlayerController : NetworkBehaviour
{
    private BotInputService botInputService;
    private HoverCarMover hoverCarMover;
    private bool isInitialized = false;

    public override void OnNetworkSpawn()
    {
        if (!IsServer)
        {
            // Bots are server-controlled, disable on clients
            enabled = false;
            return;
        }

        InitializeBot();
    }

    private void InitializeBot()
    {
        if (isInitialized)
            return;

        botInputService = new BotInputService();
        hoverCarMover = GetComponent<HoverCarMover>();

        if (hoverCarMover != null)
        {
            hoverCarMover.Construct(botInputService);
            // Enable the mover even though bot isn't an "owner" in the traditional sense
            // We'll override the owner check by ensuring the component stays enabled
            hoverCarMover.enabled = true;
        }

        isInitialized = true;
        Debug.Log($"[BotPlayerController] Bot initialized: {gameObject.name}");
    }

    private void Update()
    {
        if (!IsServer || botInputService == null)
            return;

        // Update bot AI input service (Tick is called by VContainer, but we call it manually for bots)
        botInputService.Tick();
    }

    public override void OnNetworkDespawn()
    {
        if (botInputService != null)
        {
            botInputService.Reset();
        }
    }
}

