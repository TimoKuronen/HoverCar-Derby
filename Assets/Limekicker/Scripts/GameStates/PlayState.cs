/// <summary>
/// Active round state: makes sure the race chase camera holds the view.
/// </summary>
public class PlayState : IGameState
{
    private readonly GameManager gameManager;

    public PlayState(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public void Enter()
    {
        gameManager.Context.ActivateRaceCamera();
    }

    public void Exit() { }

    public void Update() { }
}
