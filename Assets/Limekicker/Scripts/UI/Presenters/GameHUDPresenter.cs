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

        view.OnResumeClicked += HandleResumeClicked;
        view.OnRestartClicked += HandleRestartClicked;
        view.OnGoToMenuClicked += HandleGoToMenuClicked;

        if (debugInfoType != InfoTextType.None && gameManager.CurrentGameState != null)
            UpdateDebugText();

        SyncMenuVisibilityToState();
    }

    protected override void UnsubscribeFromModels()
    {
        if (gameStateChangeBinding != null)
            EventBus<GameStateChangeEvent>.Unregister(gameStateChangeBinding);

        view.OnResumeClicked -= HandleResumeClicked;
        view.OnRestartClicked -= HandleRestartClicked;
        view.OnGoToMenuClicked -= HandleGoToMenuClicked;
    }

    private void HandleGameStateChange(GameStateChangeEvent @event)
    {
        UpdateDebugText();
        SyncMenuVisibilityToState();
    }

    private void SyncMenuVisibilityToState()
    {
        var state = gameManager.CurrentGameState;
        view.ShowPauseMenu(state is PauseState);
        view.ShowWinMenu(state is RaceCompletionState);
    }

    private void HandleResumeClicked()
    {
        if (gameManager.CurrentGameState is PauseState)
            gameManager.ReturnToPreviousState();
    }

    private void HandleRestartClicked()
    {
        UnityEngine.SceneManagement.SceneManager.LoadScene(
            UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
    }

    private void HandleGoToMenuClicked()
    {
        NetworkSession.LeaveGame();
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