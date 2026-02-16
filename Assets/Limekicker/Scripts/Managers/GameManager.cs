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

    public IntVariable CountdownValue => countdownValue;

    private IGameState currentState;
    private IGameState previousState;

    private IScoreManager scoreManager;

    private PlayerSpawnManager playerSpawnManager;
    private Coroutine timerCoroutine;

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
        bool skipCountodown = MainMenu.IsSkipCountdownEnabled();
        gameTimerValue.Value = Context.roundDurationInSeconds;
        countdownValue.Value = -1; // Ensure countdown starts hidden

        Debug.Log("GameManager Initialization Started.");

        // Start in Cinematic State
        if (!skipCountodown)
        {
            CinematicState cinematicState = new CinematicState(this);
            ChangeState(cinematicState);

            yield return new WaitForSeconds(cinematicState.GetStateDuration());

            timerCoroutine = StartCoroutine(UpdateGameTimer());

            yield break;
        }

        // If skipping countdown, directly transition to PlayState after a brief delay
        yield return new WaitForSeconds(1f);

        timerCoroutine = StartCoroutine(UpdateGameTimer());

        Context.raceCamera.Priority = 20;

        ChangeState(new PlayState());
    }

    private void Update()
    {
        currentState?.Update();
    }

    private IEnumerator UpdateGameTimer()
    {
        while (true)
        {
            bool timeRunning = currentState is PlayState;

            if (timeRunning)
            {
                yield return new WaitForSeconds(1f);

                gameTimerValue.Value--;

                if (gameTimerValue.Value >= Context.roundDurationInSeconds)
                {
                    ChangeState(new RaceCompletionState(this));
                }
            }
            else
            {
                yield return null; // Wait for the next frame and re-check
            }
        }
    }

    public void ChangeState(IGameState newState)
    {
        if (newState is not PauseState && previousState is not PauseState)
            previousState = currentState;

        currentState?.Exit();
        currentState = newState;
        currentState.Enter();

        EventBus<GameStateChangeEvent>.Raise(new GameStateChangeEvent { NewState = currentState });

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
        var leadingPlayerId = scoreManager.GetLeadingPlayer().ClientId;
        return PlayerTracker.GetPlayerByID(leadingPlayerId);
    }

    public void ReturnToPreviousState()
    {
        ChangeState(previousState);
    }

    void OnDestroy()
    {
        playerSpawnManager?.Dispose();
    }
}