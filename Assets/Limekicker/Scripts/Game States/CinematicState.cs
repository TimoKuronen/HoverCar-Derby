using UnityEngine;

public class CinematicState : IGameState
{
    private readonly GameManager manager;
    private readonly Camera mainCamera;

    private string playerLayerName = "Car";

    private readonly int playerLayer;
    private float timer;
    private const float stateDuration = 3f;
    public float GetStateDuration() => stateDuration;

    public CinematicState(GameManager manager)
    {
        this.manager = manager;
        playerLayer = LayerMask.NameToLayer(playerLayerName);
        mainCamera = Camera.main;
    }

    public void Enter()
    {
        mainCamera.cullingMask &= ~(1 << playerLayer);
        manager.Context.endingDollyCamera.ToggleMovement();
    }

    public void Update()
    {
        timer += Time.deltaTime;

        if (timer > stateDuration)
        {
            manager.ChangeState(new CountdownState(manager));
        }
    }

    public void Exit()
    {
        manager.Context.endingDollyCamera.ToggleMovement();
        mainCamera.cullingMask |= 1 << playerLayer;

        timer = 0;
    }
}