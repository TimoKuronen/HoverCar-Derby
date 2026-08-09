using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

public class GameManager : NetworkBehaviour, IGameManager
{
    [Header("References")]
    [SerializeField] private RaceContext context;
    [SerializeField] private IntVariable gameTimerValue;
    [SerializeField] private IntVariable countdownValue;

    private IGameState currentState;
    private IGameState previousState;
    private IScoreManager scoreManager;
    private MatchTimerDisplaySync timerDisplaySync;
    private MatchPauseController pauseController;

    private PlayerSpawnManager playerSpawnManager;
    private Coroutine matchFlowCoroutine;

    private readonly HashSet<ulong> registeredParticipants = new();
    private readonly HashSet<ulong> readyParticipants = new();

    private readonly NetworkVariable<RaceContext.MatchPhase> matchPhase = new NetworkVariable<RaceContext.MatchPhase>(
        RaceContext.MatchPhase.WaitingForPlayers,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<double> phaseStartServerTime = new NetworkVariable<double>(
        0d,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private readonly NetworkVariable<double> roundEndServerTime = new NetworkVariable<double>(
        0d,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server);

    private RaceContext.MatchPhase lastAppliedPhase = RaceContext.MatchPhase.WaitingForPlayers;
    private bool serverMatchFlowStarted;

    public IntVariable CountdownValue => countdownValue;
    public PlayerTracker PlayerTracker { get; private set; }
    public IGameState CurrentGameState => currentState;
    public bool CanPause => pauseController != null && pauseController.CanPause;
    public RaceContext Context => context;
    public RaceContext.MatchPhase CurrentMatchPhase => matchPhase.Value;
    public double PhaseStartServerTime => phaseStartServerTime.Value;

    [Inject]
    public void Construct(IScoreManager scoreManager, IInputService inputService)
    {
        this.scoreManager = scoreManager;

        PlayerTracker = new PlayerTracker();
        playerSpawnManager = new PlayerSpawnManager(inputService, this);
        pauseController = new MatchPauseController(() => currentState, ChangeState);
        timerDisplaySync = new MatchTimerDisplaySync(
            context,
            countdownValue,
            gameTimerValue,
            matchPhase,
            phaseStartServerTime,
            roundEndServerTime);
    }

    void Start()
    {
        gameTimerValue.Value = Context.roundDurationInSeconds;
        countdownValue.Value = -1;

        StartCoroutine(playerSpawnManager.Initialize());
    }

    public override void OnNetworkSpawn()
    {
        matchPhase.OnValueChanged += HandleMatchPhaseChanged;

        if (IsServer && !serverMatchFlowStarted)
        {
            serverMatchFlowStarted = true;

            if (MainMenu.IsSkipCountdownEnabled())
            {
                matchFlowCoroutine = StartCoroutine(SkipToPlayingCoroutine());
            }
            else
            {
                double cinematicStart = NetworkManager.ServerTime.Time + Context.phaseReplicationBufferSeconds;
                SetMatchPhase(RaceContext.MatchPhase.Cinematic, cinematicStart);
                matchFlowCoroutine = StartCoroutine(ServerMatchFlowCoroutine(cinematicStart));
            }
        }

        ApplyMatchPhase(matchPhase.Value);
    }

    public override void OnNetworkDespawn()
    {
        matchPhase.OnValueChanged -= HandleMatchPhaseChanged;

        if (matchFlowCoroutine != null)
        {
            StopCoroutine(matchFlowCoroutine);
            matchFlowCoroutine = null;
        }
    }

    private void Update()
    {
        currentState?.Update();
        timerDisplaySync?.Tick();
        TryCompleteRoundFromTimer();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame)
            DevSetRemainingRoundSeconds(1);
#endif
    }

    private void TryCompleteRoundFromTimer()
    {
        if (!IsServer || matchPhase.Value != RaceContext.MatchPhase.Playing || NetworkManager.Singleton == null)
            return;

        if (roundEndServerTime.Value <= 0d)
            return;

        if (NetworkManager.ServerTime.Time >= roundEndServerTime.Value)
            SetMatchPhase(RaceContext.MatchPhase.Completed, NetworkManager.ServerTime.Time);
    }

    public void RegisterParticipant(ulong networkObjectId)
    {
        if (!IsServer)
            return;

        registeredParticipants.Add(networkObjectId);
    }

    public void MarkParticipantReady(ulong networkObjectId)
    {
        if (!IsServer)
            return;

        if (!registeredParticipants.Contains(networkObjectId))
            return;

        readyParticipants.Add(networkObjectId);
    }

    public void UnregisterParticipant(ulong networkObjectId)
    {
        if (!IsServer)
            return;

        registeredParticipants.Remove(networkObjectId);
        readyParticipants.Remove(networkObjectId);
    }

    [ServerRpc(RequireOwnership = false)]
    public void ReportPlayerReadyServerRpc(ulong participantNetworkObjectId, ServerRpcParams rpcParams = default)
    {
        if (NetworkManager.SpawnManager == null ||
            !NetworkManager.SpawnManager.SpawnedObjects.TryGetValue(participantNetworkObjectId, out NetworkObject netObj))
        {
            return;
        }

        if (netObj.OwnerClientId != rpcParams.Receive.SenderClientId)
            return;

        MarkParticipantReady(participantNetworkObjectId);
    }

    private IEnumerator ServerMatchFlowCoroutine(double cinematicStart)
    {
        yield return new WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening);

        yield return new WaitUntil(() =>
            HasRequiredReadyParticipants() &&
            NetworkManager.ServerTime.Time >= cinematicStart + Context.cinematicDurationSeconds);

        double countdownStart = NetworkManager.ServerTime.Time;
        SetMatchPhase(RaceContext.MatchPhase.Countdown, countdownStart);

        float countdownDuration = Context.countdownIntervalSeconds * 3f + Context.countdownGoDelaySeconds;
        yield return new WaitUntil(() =>
            NetworkManager.ServerTime.Time >= countdownStart + countdownDuration);

        double playStart = NetworkManager.ServerTime.Time;
        roundEndServerTime.Value = playStart + Context.roundDurationInSeconds;
        SetMatchPhase(RaceContext.MatchPhase.Playing, playStart);

        yield return new WaitUntil(() => NetworkManager.ServerTime.Time >= roundEndServerTime.Value);

        SetMatchPhase(RaceContext.MatchPhase.Completed, NetworkManager.ServerTime.Time);
    }

    private IEnumerator SkipToPlayingCoroutine()
    {
        yield return new WaitUntil(() => NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening);

        double playStart = NetworkManager.ServerTime.Time;
        roundEndServerTime.Value = playStart + Context.roundDurationInSeconds;
        SetMatchPhase(RaceContext.MatchPhase.Playing, playStart);

        yield return new WaitUntil(() => NetworkManager.ServerTime.Time >= roundEndServerTime.Value);

        SetMatchPhase(RaceContext.MatchPhase.Completed, NetworkManager.ServerTime.Time);
    }

    private bool HasRequiredReadyParticipants()
    {
        return readyParticipants.Count >= Context.requiredPlayerCount;
    }

    private void SetMatchPhase(RaceContext.MatchPhase phase, double startServerTime)
    {
        phaseStartServerTime.Value = startServerTime;
        matchPhase.Value = phase;
    }

    private void HandleMatchPhaseChanged(RaceContext.MatchPhase previous, RaceContext.MatchPhase current)
    {
        ApplyMatchPhase(current);
    }

    private void ApplyMatchPhase(RaceContext.MatchPhase phase)
    {
        if (phase == lastAppliedPhase && currentState != null)
            return;

        lastAppliedPhase = phase;

        IGameState newState = phase switch
        {
            RaceContext.MatchPhase.WaitingForPlayers => new IdleState(),
            RaceContext.MatchPhase.Cinematic => new CinematicState(this),
            RaceContext.MatchPhase.Countdown => new CountdownState(this),
            RaceContext.MatchPhase.Playing => new PlayState(),
            RaceContext.MatchPhase.Completed => new RaceCompletionState(this),
            _ => new IdleState()
        };

        ChangeState(newState);
    }

    public void ChangeState(IGameState newState)
    {
        if (newState is not PauseState && currentState is not PauseState)
            previousState = currentState;

        currentState?.Exit();
        currentState = newState;
        currentState.Enter();

        EventBus<GameStateChangeEvent>.Raise(new GameStateChangeEvent { NewState = currentState });
    }

    public NetworkObject GetLeadingPlayer()
    {
        var ranked = scoreManager.GetRankedPlayersByScore();
        if (ranked == null || ranked.Count == 0)
            return null;
        var leadingPlayerId = scoreManager.GetLeadingPlayer().ClientId;
        return PlayerTracker.GetPlayerByScoreClientId(leadingPlayerId);
    }

    public void ReturnToPreviousState()
    {
        if (pauseController != null && pauseController.TryResumeFromPause())
            return;

        if (previousState != null)
            ChangeState(previousState);
    }

    public void TogglePause()
    {
        pauseController?.TogglePause();
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void DevSetRemainingRoundSeconds(int seconds)
    {
        if (!IsServer || matchPhase.Value != RaceContext.MatchPhase.Playing || NetworkManager.Singleton == null)
            return;

        roundEndServerTime.Value = NetworkManager.ServerTime.Time + seconds;
    }
#endif

    public override void OnDestroy()
    {
        playerSpawnManager?.Dispose();

        if (matchFlowCoroutine != null)
            StopCoroutine(matchFlowCoroutine);

        base.OnDestroy();
    }
}
