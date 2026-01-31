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

    private IGameState currentState;
    private IGameState previousState;
    private IScoreManager scoreManager;

    private PlayerSpawnManager playerSpawnManager;
    private Coroutine timerCoroutine;
    private int gameTimer = 0;

    public PlayerTracker PlayerTracker { get; private set; }
    public RaceContext Context => context;
    public float GameTimeLeft => gameTimer;

    public event Action<int> OnGameTimerUpdated;

    [Inject]
    public void Construct(IScoreManager scoreManager, IInputService inputService)
    {
        this.scoreManager = scoreManager;
        PlayerTracker = new PlayerTracker();

        playerSpawnManager = new PlayerSpawnManager(inputService, this);
    }

    void Start()
    {
        CoroutineMonoBehavior.Instance.StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
        Debug.Log("GameManager Initialization Started.");
        // Start in Cinematic State
        // ChangeState(new CinematicState(this));

        yield return new WaitForSeconds(1);
        // For now, start directly in Play State
        Context.raceCamera.Priority = 20;

        timerCoroutine = StartCoroutine(UpdateGameTimer());

        ChangeState(new PlayState());
    }

    private void Update()
    {
        currentState?.Update();

        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            gameTimer = Context.roundDurationInSeconds;
        }
    }

    private IEnumerator UpdateGameTimer()
    {
        while (true)
        {
            bool timeRunning = currentState is PlayState;

            if (timeRunning)
            {
                yield return new WaitForSeconds(1f);

                gameTimer += 1;
                OnGameTimerUpdated?.Invoke(Context.roundDurationInSeconds - gameTimer);

                if (gameTimer >= Context.roundDurationInSeconds)
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
