using System.Collections;
using Unity.Netcode;
using UnityEngine;

public class CinematicState : IGameState
{
    private readonly GameManager manager;
    private readonly Camera mainCamera;

    private string playerLayerName = "Car";

    private readonly int playerLayer;
    private Coroutine beginCinematicCoroutine;
    private bool cinematicStarted;

    public CinematicState(GameManager manager)
    {
        this.manager = manager;
        playerLayer = LayerMask.NameToLayer(playerLayerName);
        mainCamera = Camera.main;
    }

    /// <summary>
    /// Waits for the server-scheduled cinematic start time, then hides players and runs the dolly.
    /// </summary>
    public void Enter()
    {
        beginCinematicCoroutine = CoroutineMonoBehavior.Instance.StartCoroutine(BeginCinematicWhenScheduled());
    }

    public void Update() { }

    public void Exit()
    {
        if (beginCinematicCoroutine != null)
        {
            CoroutineMonoBehavior.Instance.StopCoroutine(beginCinematicCoroutine);
            beginCinematicCoroutine = null;
        }

        if (cinematicStarted)
        {
            manager.Context.endingDollyCamera.ToggleMovement();
            cinematicStarted = false;
        }

        mainCamera.cullingMask |= 1 << playerLayer;
    }

    private IEnumerator BeginCinematicWhenScheduled()
    {
        yield return new WaitUntil(() =>
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.ServerTime.Time >= manager.PhaseStartServerTime);

        cinematicStarted = true;
        mainCamera.cullingMask &= ~(1 << playerLayer);
        manager.Context.endingDollyCamera.ToggleMovement();
    }
}
