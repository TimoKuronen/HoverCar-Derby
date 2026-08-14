/// <summary>
/// Toggles gameplay HUD visibility based on game state.
/// </summary>
public class GameplayHudPresenter : BasePresenter
{
    private readonly IGameplayHudView view;
    private readonly IGameManager gameManager;
    private EventBinding<GameStateChangeEvent> gameStateChangeBinding;

    public GameplayHudPresenter(IGameplayHudView view, IGameManager gameManager)
    {
        this.view = view;
        this.gameManager = gameManager;
    }

    protected override void SubscribeToModels()
    {
        gameStateChangeBinding = new EventBinding<GameStateChangeEvent>(HandleGameStateChange);
        EventBus<GameStateChangeEvent>.Register(gameStateChangeBinding);

        SyncToCurrentState(gameManager?.CurrentGameState);
    }

    protected override void UnsubscribeFromModels()
    {
        if (gameStateChangeBinding != null)
            EventBus<GameStateChangeEvent>.Unregister(gameStateChangeBinding);
    }

    private void HandleGameStateChange(GameStateChangeEvent @event)
    {
        SyncToCurrentState(@event.NewState);
    }

    private void SyncToCurrentState(IGameState state)
    {
        view.SetGameplayHudVisible(state is CountdownState or PlayState or PauseState);
        view.SetDrivingControlsVisible(state is PlayState);
    }
}
