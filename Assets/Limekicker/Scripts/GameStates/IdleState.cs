/// <summary>
/// Pre-match state: the intro dolly camera holds the view until the cinematic starts.
/// </summary>
public class IdleState : IGameState
{
    private readonly GameManager gameManager;

    public IdleState(GameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    public void Enter()
    {
        gameManager.Context.ShowIntroCamera();
    }

    public void Exit() { }

    public void Update() { }
}
