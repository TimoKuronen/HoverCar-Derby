using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

public class GameManager : MonoBehaviour, IGameManager
{
    [Header("References")]
    [SerializeField] private RaceContext context;
    [SerializeField] private IntVariable gameTimerValue;
    [SerializeField] private IntVariable countdownValue;

    private IGameState currentState;
    private IGameState previousState;
    private IScoreManager scoreManager;

    private PlayerSpawnManager playerSpawnManager;
    private Coroutine timerCoroutine;

    public IntVariable CountdownValue => countdownValue;
    public PlayerTracker PlayerTracker { get; private set; }
    public IGameState CurrentGameState => currentState;
    public RaceContext Context => context;

    [Inject]
    public void Construct(IScoreManager scoreManager, IInputService inputService)
    {
        this.scoreManager = scoreManager;

        PlayerTracker = new PlayerTracker();
        playerSpawnManager = new PlayerSpawnManager(inputService, this);
    }

    void Start()
    {
        StartCoroutine(Initialize());
        StartCoroutine(playerSpawnManager.Initialize());
    }

    private IEnumerator Initialize()
    {
        bool skipCountdown = MainMenu.IsSkipCountdownEnabled();
        gameTimerValue.Value = Context.roundDurationInSeconds;
        countdownValue.Value = -1;

        Debug.Log("GameManager Initialization Started.");

        if (!skipCountdown)
        {
            CinematicState cinematicState = new CinematicState(this);
            ChangeState(cinematicState);

            yield return new WaitForSeconds(cinematicState.GetStateDuration());

            timerCoroutine = StartCoroutine(UpdateGameTimer());

            yield break;
        }

        yield return new WaitForSeconds(1f);

        timerCoroutine = StartCoroutine(UpdateGameTimer());

        Context.raceCamera.Priority = 20;

        ChangeState(new PlayState());
    }

    private void Update()
    {
        currentState?.Update();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (Keyboard.current != null && Keyboard.current.yKey.wasPressedThisFrame)
            gameTimerValue.Value = 2;
#endif
    }

    /// <summary>
    /// Coroutine that updates the game timer. Only decrements when in PlayState.
    /// Transitions to RaceCompletionState when timer reaches or exceeds round duration.
    /// </summary>
    private IEnumerator UpdateGameTimer()
    {
        while (true)
        {
            bool timeRunning = currentState is PlayState;

            if (timeRunning)
            {
                yield return new WaitForSeconds(1f);

                gameTimerValue.Value--;

                if (gameTimerValue.Value <= 0)
                {
                    ChangeState(new RaceCompletionState(this));
                }
            }
            else
            {
                yield return null;
            }
        }
    }

    /// <summary>
    /// Changes the current game state. Preserves previous state unless entering/exiting pause.
    /// Stops timer coroutine when entering completion state.
    /// </summary>
    public void ChangeState(IGameState newState)
    {
        // Don't overwrite previous state when pausing/unpausing
        if (newState is not PauseState && previousState is not PauseState)
            previousState = currentState;

        currentState?.Exit();
        currentState = newState;
        currentState.Enter();

        EventBus<GameStateChangeEvent>.Raise(new GameStateChangeEvent { NewState = currentState });

        // Stop timer when race completes
        if (newState is RaceCompletionState)
        {
            if (timerCoroutine != null)
            {
                StopCoroutine(timerCoroutine);
                timerCoroutine = null;
            }
        }
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
        ChangeState(previousState);
    }

    void OnDestroy()
    {
        playerSpawnManager?.Dispose();
        if (timerCoroutine != null)
        {
            StopCoroutine(timerCoroutine);
        }
    }
}