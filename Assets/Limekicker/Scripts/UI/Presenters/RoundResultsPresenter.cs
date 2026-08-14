using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Shows round results and handles rematch or return to menu.
/// </summary>
public class RoundResultsPresenter : BasePresenter
{
    private readonly IRoundResultsView view;
    private readonly IGameManager gameManager;
    private EventBinding<GameStateChangeEvent> gameStateChangeBinding;

    public RoundResultsPresenter(
        IRoundResultsView view,
        IGameManager gameManager)
    {
        this.view = view;
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
        IReadOnlyList<PlayerData> ranked = GatherRankedPlayers();
        view.ClearScoreRows();

        foreach (PlayerData player in ranked)
            view.AddScoreRow(player.ClientId, player.PlayerName.ToString(), player.Points);

        view.ShowResults(BuildResultTitle(ranked));
    }

    private IReadOnlyList<PlayerData> GatherRankedPlayers()
    {
        var list = new List<PlayerData>();
        PlayerController[] controllers = Object.FindObjectsByType<PlayerController>(FindObjectsSortMode.None);

        foreach (PlayerController controller in controllers)
        {
            if (controller == null || !controller.IsSpawned)
                continue;

            ulong clientId = controller.IsBot ? controller.NetworkObjectId : controller.OwnerClientId;
            list.Add(new PlayerData
            {
                ClientId = clientId,
                PlayerName = controller.PlayerName.Value,
                Points = controller.Score.Value
            });
        }

        list.Sort((a, b) => b.Points.CompareTo(a.Points));
        return list;
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

        if (topScore == 0)
        {
            if (tiedAtTop > 1 && localTiedAtTop)
                return "Draw!";

            return "Round Over";
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
