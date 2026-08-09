using System.Collections.Generic;
using Unity.Netcode;

public class RoundResultsPresenter : BasePresenter
{
    private readonly IRoundResultsView view;
    private readonly IScoreManager scoreManager;
    private readonly IGameManager gameManager;
    private EventBinding<GameStateChangeEvent> gameStateChangeBinding;

    public RoundResultsPresenter(
        IRoundResultsView view,
        IScoreManager scoreManager,
        IGameManager gameManager)
    {
        this.view = view;
        this.scoreManager = scoreManager;
        this.gameManager = gameManager;
    }

    protected override void SubscribeToModels()
    {
        gameStateChangeBinding = new EventBinding<GameStateChangeEvent>(HandleGameStateChange);
        EventBus<GameStateChangeEvent>.Register(gameStateChangeBinding);

        view.OnRematchClicked += HandleRematchClicked;
        view.OnMainMenuClicked += HandleMainMenuClicked;

        SyncToCurrentState(gameManager?.CurrentGameState);
    }

    protected override void UnsubscribeFromModels()
    {
        if (gameStateChangeBinding != null)
            EventBus<GameStateChangeEvent>.Unregister(gameStateChangeBinding);

        view.OnRematchClicked -= HandleRematchClicked;
        view.OnMainMenuClicked -= HandleMainMenuClicked;
    }

    private void HandleGameStateChange(GameStateChangeEvent @event)
    {
        SyncToCurrentState(@event.NewState);
    }

    private void SyncToCurrentState(IGameState state)
    {
        if (state is RaceCompletionState)
            ShowRoundResults();
        else
            view.HideResults();
    }

    private void ShowRoundResults()
    {
        if (scoreManager == null)
        {
            view.ShowResults("Round Over");
            return;
        }

        IReadOnlyList<PlayerData> ranked = scoreManager.GetRankedPlayersByScore();
        view.ClearScoreRows();

        foreach (PlayerData player in ranked)
            view.AddScoreRow(player.ClientId, player.PlayerName.ToString(), player.Points);

        view.ShowResults(BuildResultTitle(ranked));
    }

    private static string BuildResultTitle(IReadOnlyList<PlayerData> ranked)
    {
        if (ranked == null || ranked.Count == 0)
            return "Round Over";

        int topScore = ranked[0].Points;
        int tiedAtTop = 0;
        bool localTiedAtTop = false;
        ulong localClientId = NetworkManager.Singleton != null
            ? NetworkManager.Singleton.LocalClientId
            : ulong.MaxValue;

        foreach (PlayerData player in ranked)
        {
            if (player.Points != topScore)
                break;

            tiedAtTop++;
            if (player.ClientId == localClientId)
                localTiedAtTop = true;
        }

        if (tiedAtTop > 1 && localTiedAtTop)
            return "Draw!";

        if (ranked[0].ClientId == localClientId)
            return "You Win!";

        return "You Lose!";
    }

    private void HandleRematchClicked()
    {
        NetworkSession.RestartCurrentMatch();
    }

    private void HandleMainMenuClicked()
    {
        NetworkSession.ReturnToMainMenu();
    }
}
