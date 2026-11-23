using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Server-side sanity check for an owner-driven car. The server samples the replicated transform, rejects
/// horizontal speeds above the car's cap and positions outside the play volume by sending the owner back to
/// the last accepted position, and removes a client that keeps offending. Bots and the host's own car are
/// exempt, since the host is the server.
/// </summary>
public class ServerMovementGuard : NetworkBehaviour
{
    private const float MaxSampleGapSeconds = 1f;
    private const float TeleportArrivalRadius = 3f;
    private const float TeleportExpectationSeconds = 3f;
    private const float SnapBackGraceSeconds = 1f;

    [SerializeField] private MapData mapData;

    [Header("Speed")]
    [SerializeField] private float sampleIntervalSeconds = 0.25f;
    [Tooltip("Multiplier on the car's speed cap to absorb collision impulses and network smoothing.")]
    [SerializeField] private float speedTolerance = 1.5f;

    [Header("Play volume")]
    [SerializeField] private float horizontalMargin = 15f;
    [SerializeField] private float belowGroundMargin = 30f;
    [SerializeField] private float aboveGroundMargin = 40f;

    [Header("Enforcement")]
    [SerializeField] private int violationsBeforeKick = 5;
    [SerializeField] private float violationWindowSeconds = 30f;

    private HoverCarMover mover;
    private PlayerController playerController;
    private MovementViolationLog violationLog;

    private bool hasBaseline;
    private Vector3 baselinePosition;
    private Quaternion baselineRotation;
    private float baselineTime;
    private float nextSampleTime;

    private bool hasExpectedTeleport;
    private Vector3 expectedTeleportTarget;
    private float expectedTeleportUntil;
    private float snapBackGraceUntil;

    public override void OnNetworkSpawn()
    {
        mover = GetComponent<HoverCarMover>();
        playerController = GetComponent<PlayerController>();

        bool isBot = GetComponent<BotPlayerController>() != null;
        bool isHostCar = IsOwner;
        if (!IsServer || isBot || isHostCar || mover == null || playerController == null)
        {
            enabled = false;
            return;
        }

        if (mapData == null)
        {
            Debug.LogWarning("[ServerMovementGuard] No MapData assigned; the play volume check is off.");
        }

        violationLog = new MovementViolationLog(violationsBeforeKick, violationWindowSeconds);
    }

    /// <summary>
    /// Announces a server-ordered teleport so the owner's jump to the target is accepted when it arrives.
    /// </summary>
    public void ExpectTeleport(Vector3 target)
    {
        hasExpectedTeleport = true;
        expectedTeleportTarget = target;
        expectedTeleportUntil = Time.unscaledTime + TeleportExpectationSeconds;
    }

    private void Update()
    {
        float now = Time.unscaledTime;
        if (now < nextSampleTime)
            return;

        nextSampleTime = now + sampleIntervalSeconds;
        Vector3 position = transform.position;

        if (!hasBaseline || now - baselineTime > MaxSampleGapSeconds)
        {
            AcceptSample(position, now);
            return;
        }

        if (TryAcceptTeleportArrival(position, now))
            return;

        if (now < snapBackGraceUntil)
            return;

        MovementVerdict verdict = MovementRules.Evaluate(baselinePosition, position, now - baselineTime, BuildLimits());
        if (verdict == MovementVerdict.Accepted)
        {
            AcceptSample(position, now);
            return;
        }

        HandleViolation(verdict, position, now);
    }

    private bool TryAcceptTeleportArrival(Vector3 position, float now)
    {
        if (!hasExpectedTeleport)
            return false;

        if (now > expectedTeleportUntil)
        {
            hasExpectedTeleport = false;
            return false;
        }

        if (Vector3.Distance(position, expectedTeleportTarget) > TeleportArrivalRadius)
            return false;

        hasExpectedTeleport = false;
        snapBackGraceUntil = 0f;
        AcceptSample(position, now);
        return true;
    }

    private void AcceptSample(Vector3 position, float now)
    {
        hasBaseline = true;
        baselinePosition = position;
        baselineRotation = transform.rotation;
        baselineTime = now;
    }

    private void HandleViolation(MovementVerdict verdict, Vector3 position, float now)
    {
        Debug.LogWarning($"[ServerMovementGuard] Client {OwnerClientId} rejected ({verdict}): " +
                         $"{baselinePosition} -> {position}.");

        // Keep the last accepted position but restart its clock, so a client that ignores the snap-back
        // keeps measuring as too fast instead of its speed averaging down over time.
        baselineTime = now;

        if (violationLog.Register(now))
        {
            NetworkManager.DisconnectClient(OwnerClientId, "Rejected: invalid movement.");
            enabled = false;
            return;
        }

        ExpectTeleport(baselinePosition);
        snapBackGraceUntil = now + SnapBackGraceSeconds;
        playerController.TeleportToSpawnPositionClientRpc(baselinePosition, baselineRotation);
    }

    private MovementLimits BuildLimits()
    {
        return new MovementLimits(mover.SpeedCap * speedTolerance, BuildPlayVolume());
    }

    private Bounds? BuildPlayVolume()
    {
        if (mapData == null)
            return null;

        Vector2 half = mapData.Size * 0.5f + Vector2.one * horizontalMargin;
        Vector3 min = new Vector3(
            mapData.Center.x - half.x,
            mapData.MinGroundHeight - belowGroundMargin,
            mapData.Center.y - half.y);
        Vector3 max = new Vector3(
            mapData.Center.x + half.x,
            mapData.MaxGroundHeight + aboveGroundMargin,
            mapData.Center.y + half.y);

        Bounds volume = new Bounds();
        volume.SetMinMax(min, max);
        return volume;
    }
}
