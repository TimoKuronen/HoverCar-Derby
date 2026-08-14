using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Waits for the server-scheduled start time, then prepares the race camera for countdown.
/// </summary>
public class CountdownState : IGameState
{
    private readonly GameManager gameManager;
    private Coroutine beginCountdownCoroutine;

    public CountdownState(GameManager manager)
    {
        gameManager = manager;
    }

    public void Enter()
    {
        beginCountdownCoroutine = CoroutineMonoBehavior.Instance.StartCoroutine(BeginCountdownWhenScheduled());
    }

    public void Exit()
    {
        if (beginCountdownCoroutine != null)
        {
            CoroutineMonoBehavior.Instance.StopCoroutine(beginCountdownCoroutine);
            beginCountdownCoroutine = null;
        }

        gameManager.CountdownValue.Value = -1;
    }

    public void Update() { }

    private IEnumerator BeginCountdownWhenScheduled()
    {
        yield return new WaitUntil(() =>
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.ServerTime.Time >= gameManager.PhaseStartServerTime);

        gameManager.Context.raceCamera.Priority = 20;
    }
}
