/// <summary>
/// Syncs pause menu visibility and actions with game state.
/// </summary>
public class PauseMenuPresenter : BasePresenter
{
    private readonly IPauseMenuView view;
    private readonly IGameManager gameManager;
    private EventBinding<GameStateChangeEvent> gameStateChangeBinding;

    public PauseMenuPresenter(IPauseMenuView view, IGameManager gameManager)
    {
        this.view = view;
        this.gameManager = gameManager;
    }

    protected override void SubscribeToModels()
    {
        gameStateChangeBinding = new EventBinding<GameStateChangeEvent>(HandleGameStateChange);
        EventBus<GameStateChangeEvent>.Register(gameStateChangeBinding);

        view.OnPauseMenuClicked += HandlePauseMenuClicked;
        view.OnResumeClicked += HandleResumeClicked;
        view.OnGoToMenuClicked += HandleGoToMenuClicked;

        SyncMenuVisibilityToState();
    }

    protected override void UnsubscribeFromModels()
    {
        if (gameStateChangeBinding != null)
            EventBus<GameStateChangeEvent>.Unregister(gameStateChangeBinding);

        view.OnPauseMenuClicked -= HandlePauseMenuClicked;
        view.OnResumeClicked -= HandleResumeClicked;
        view.OnGoToMenuClicked -= HandleGoToMenuClicked;
    }

    private void HandleGameStateChange(GameStateChangeEvent @event)
    {
        SyncMenuVisibilityToState();
    }

    private void SyncMenuVisibilityToState()
    {
        view.ShowPauseMenu(gameManager.CurrentGameState is PauseState);
    }

    private void HandlePauseMenuClicked()
    {
        gameManager.TogglePause();
    }

    private void HandleResumeClicked()
    {
        if (gameManager.CurrentGameState is PauseState)
            gameManager.ReturnToPreviousState();
    }

    private void HandleGoToMenuClicked()
    {
        NetworkSession.ReturnToMainMenu();
    }
}
