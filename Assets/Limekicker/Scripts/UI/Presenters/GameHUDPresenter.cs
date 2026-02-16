public class GameHUDPresenter : BasePresenter
{
    private readonly IGameHUDView view;
    private readonly IGameManager gameManager;
    private readonly InfoTextType debugInfoType;
    private EventBinding<GameStateChangeEvent> gameStateChangeBinding;

    public GameHUDPresenter(IGameHUDView view, IGameManager gameManager, InfoTextType debugInfoType)
    {
        this.view = view;
        this.gameManager = gameManager;
        this.debugInfoType = debugInfoType;
    }

    protected override void SubscribeToModels()
    {
        gameStateChangeBinding = new EventBinding<GameStateChangeEvent>(HandleGameStateChange);
        EventBus<GameStateChangeEvent>.Register(gameStateChangeBinding);

        if (debugInfoType != InfoTextType.None && gameManager.CurrentGameState != null)
        {
            UpdateDebugText();
        }
    }

    protected override void UnsubscribeFromModels()
    {
        if (gameStateChangeBinding != null)
        {
            EventBus<GameStateChangeEvent>.Unregister(gameStateChangeBinding);
        }
    }

    private void HandleGameStateChange(GameStateChangeEvent @event)
    {
        UpdateDebugText();
    }

    private void UpdateDebugText()
    {
        if (debugInfoType == InfoTextType.None || gameManager.CurrentGameState == null)
        {
            view.SetDebugText("");
            return;
        }

        switch (debugInfoType)
        {
            case InfoTextType.GameState:
                view.SetDebugText(gameManager.CurrentGameState.ToString());
                break;
            default:
                view.SetDebugText("");
                break;
        }
    }
}

public enum InfoTextType
{
    None,
    GameState
}