internal class PlayState : IGameState
{
    private const int RaceCameraPlayPriority = 20;

    private readonly GameManager gameManager;

    public PlayState(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public void Enter()
    {
        if (gameManager.Context?.raceCamera == null)
            return;

        gameManager.Context.raceCamera.Priority = RaceCameraPlayPriority;
    }

    public void Exit() { }

    public void Update() { }
}
