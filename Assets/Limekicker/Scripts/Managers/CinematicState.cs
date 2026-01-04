using UnityEngine;

public class CinematicState : IGameState
{
    private readonly GameManager manager;
    private readonly Camera mainCamera;

    private string playerLayerName = "Car";

    private int playerLayer;
    private float timer;

    public CinematicState(GameManager manager)
    {
        this.manager = manager;
        playerLayer = LayerMask.NameToLayer(playerLayerName);
        mainCamera = Camera.main;
    }

    public void Enter()
    {
        mainCamera.cullingMask &= ~(1 << playerLayer);
        manager.Context.DollyCamera.ToggleMovement();
    }

    public void Update()
    {
        timer += Time.deltaTime;

        if (timer > 5 && GameSignals.IsSessionLoaded)
        {
            manager.ChangeState(new CountdownState(manager));
        }
    }

    public void Exit()
    {
        manager.Context.DollyCamera.ToggleMovement();
        mainCamera.cullingMask |= 1 << playerLayer;
    }
}