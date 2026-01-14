using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

public interface IGameState
{
    void Enter();
    void Update();
    void Exit();
}

public class GameManager : MonoBehaviour, IGameManager
{
    [Header("References")]
    [SerializeField] private RaceContext context;
    public RaceContext Context => context;

    private IGameState currentState;
    private IGameState previousState;

    private int gameTimer = 0;
    private Coroutine timerCoroutine;

    public event Action<int> OnGameTimerUpdated;

    private IPlayerSpawnManager playerSpawnManager;
    public IScoreManager scoreManager { get; private set; }
    public PlayerTracker playerTracker { get; private set; }

    [Inject]
    public void Construct(IScoreManager scoreManager, IPlayerSpawnManager playerSpawnManager)
    {
        this.scoreManager = scoreManager;
        this.playerTracker = new PlayerTracker(playerSpawnManager);

        CoroutineMonoBehavior.Instance.StartCoroutine(Initialize());
    }

    private IEnumerator Initialize()
    {
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
        Debug.Log("Game Timer Coroutine Started.");
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
        Debug.Log("Game Timer Coroutine Ended.");
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

    public void ReturnToPreviousState()
    {
        ChangeState(previousState);
    }
}
