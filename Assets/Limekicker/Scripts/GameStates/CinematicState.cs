using System.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Runs the intro dolly cinematic after the server start time is reached. The intro camera keeps the
/// view until the countdown hands it to the race camera.
/// </summary>
public class CinematicState : IGameState
{
    private const string PlayerLayerName = "Car";

    private readonly GameManager manager;
    private readonly int playerLayer;
    private Coroutine beginCinematicCoroutine;
    private bool playersHidden;

    public CinematicState(GameManager manager)
    {
        this.manager = manager;
        playerLayer = LayerMask.NameToLayer(PlayerLayerName);
        if (playerLayer < 0)
            Debug.LogError($"Layer '{PlayerLayerName}' does not exist; cars stay visible during the cinematic.");
    }

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

        if (playersHidden)
        {
            SetPlayersVisible(true);
            playersHidden = false;
        }
    }

    private IEnumerator BeginCinematicWhenScheduled()
    {
        yield return new WaitUntil(() =>
            NetworkManager.Singleton != null &&
            NetworkManager.Singleton.ServerTime.Time >= manager.PhaseStartServerTime);

        SetPlayersVisible(false);
        playersHidden = true;
        manager.Context.IntroDollyCamera.Play();
    }

    private void SetPlayersVisible(bool visible)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null || playerLayer < 0)
            return;

        int layerBit = 1 << playerLayer;
        mainCamera.cullingMask = visible ? mainCamera.cullingMask | layerBit : mainCamera.cullingMask & ~layerBit;
    }
}
