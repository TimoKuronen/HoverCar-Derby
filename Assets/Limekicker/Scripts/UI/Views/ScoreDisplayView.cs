using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer;

public class ScoreDisplayView : MonoBehaviour, IScoreDisplayView
{
    [SerializeField] private LeaderboardEntity leaderboardEntityPrefab;
    [SerializeField] private Transform scoreContainer;
    [SerializeField] private RectTransform panelToMove;

    private IScoreManager scoreManager;
    private Dictionary<ulong, LeaderboardEntity> playerScores = new Dictionary<ulong, LeaderboardEntity>();
    private ScoreDisplayPresenter presenter;

    private bool gamePositionSaved;
    private Vector2 savedAnchorMin;
    private Vector2 savedAnchorMax;
    private Vector2 savedAnchoredPosition;

    [Inject]
    public void Construct(IScoreManager scoreManager)
    {
        this.scoreManager = scoreManager;
    }

    private void Start()
    {
        TryResolveDependencies();

        if (scoreManager == null)
        {
            Debug.LogError("[ScoreDisplayView] IScoreManager was not resolved. Check GameLifetimeScope injection.");
            return;
        }

        presenter = new ScoreDisplayPresenter(this, scoreManager, this);
        presenter.Initialize();
    }

    private void TryResolveDependencies()
    {
        if (scoreManager != null)
            return;

        GameLifetimeScope scope = FindFirstObjectByType<GameLifetimeScope>();
        scope?.Container.Inject(this);
    }

    public void AddPlayer(ulong clientId, string playerName, int initialScore)
    {
        if (playerScores.ContainsKey(clientId))
        {
            playerScores[clientId].Initialise(clientId, playerName, initialScore);
            return;
        }

        playerScores.Add(clientId, Instantiate(leaderboardEntityPrefab, scoreContainer));
        playerScores[clientId].Initialise(clientId, playerName, initialScore);
    }

    public void UpdatePlayerScore(ulong clientId, int newScore)
    {
        if (playerScores.TryGetValue(clientId, out var entity))
        {
            entity.UpdatePoints(newScore);
        }
    }

    public void MoveToCenter()
    {
        if (!gamePositionSaved)
        {
            savedAnchorMin = panelToMove.anchorMin;
            savedAnchorMax = panelToMove.anchorMax;
            savedAnchoredPosition = panelToMove.anchoredPosition;
            gamePositionSaved = true;
        }

        SortEntriesByScoreDescending();

        panelToMove.anchorMin = new Vector2(0.5f, 0.5f);
        panelToMove.anchorMax = new Vector2(0.5f, 0.5f);
        panelToMove.anchoredPosition = Vector2.zero;
        panelToMove.pivot = new Vector2(0.5f, 0.5f);
    }

    public void ResetToGamePosition()
    {
        if (!gamePositionSaved)
            return;

        panelToMove.anchorMin = savedAnchorMin;
        panelToMove.anchorMax = savedAnchorMax;
        panelToMove.anchoredPosition = savedAnchoredPosition;
    }

    private void SortEntriesByScoreDescending()
    {
        var sorted = playerScores.Values.OrderByDescending(e => e.Points).ToList();
        for (int i = 0; i < sorted.Count; i++)
        {
            sorted[i].transform.SetSiblingIndex(i);
            sorted[i].UpdateText();
        }
    }

    private void OnDestroy()
    {
        presenter?.Dispose();
    }
}
