using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class ScoreDisplayView : MonoBehaviour, IScoreDisplayView
{
    [SerializeField] private LeaderboardEntity leaderboardEntityPrefab;
    [SerializeField] private Transform container;

    private IScoreManager scoreManager;
    private Dictionary<ulong, LeaderboardEntity> playerScores = new Dictionary<ulong, LeaderboardEntity>();
    private ScoreDisplayPresenter presenter;

    [Inject]
    public void Construct(IScoreManager scoreManager)
    {
        this.scoreManager = scoreManager;
    }

    private void Start()
    {
        presenter = new ScoreDisplayPresenter(this, scoreManager, this);
        presenter.Initialize();
    }

    public void AddPlayer(ulong clientId, string playerName, int initialScore)
    {
        if (playerScores.ContainsKey(clientId))
        {
            playerScores[clientId].Initialise(clientId, playerName, initialScore);
            return;
        }
        
        playerScores.Add(clientId, Instantiate(leaderboardEntityPrefab, container));
        playerScores[clientId].Initialise(clientId, playerName, initialScore);
    }

    public void UpdatePlayerScore(ulong clientId, int newScore)
    {
        if (playerScores.TryGetValue(clientId, out var entity))
        {
            entity.UpdatePoints(newScore);
        }
    }

    private void OnDestroy()
    {
        presenter?.Dispose();
    }
}
