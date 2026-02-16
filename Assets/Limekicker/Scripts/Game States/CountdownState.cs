using System.Collections;
using UnityEngine;

public class CountdownState : IGameState
{
    private readonly GameManager gameManager;
    
    private const float countdownInterval = 0.75f;
    private const float goDelay = 0.5f;

    public CountdownState(GameManager manager)
    {
        gameManager = manager;
    }

    public void Enter()
    {
        gameManager.Context.raceCamera.Priority = 20;
        CoroutineMonoBehavior.Instance.StartCoroutine(CountdownCoroutine());
    }

    public void Exit() 
    {
        // Reset countdown to invalid value to hide display
        gameManager.CountdownValue.Value = -1;
    }

    public void Update() { }

    private IEnumerator CountdownCoroutine()
    {
        // Wait a frame to ensure CountdownDisplay has subscribed to the variable
        yield return null;

        // Countdown: 3, 2, 1, then 0 for GO
        gameManager.CountdownValue.Value = 3;
        yield return new WaitForSeconds(countdownInterval);

        gameManager.CountdownValue.Value = 2;
        yield return new WaitForSeconds(countdownInterval);

        gameManager.CountdownValue.Value = 1;
        yield return new WaitForSeconds(countdownInterval);

        gameManager.CountdownValue.Value = 0;
        yield return new WaitForSeconds(goDelay);

        gameManager.ChangeState(new PlayState());
    }
}