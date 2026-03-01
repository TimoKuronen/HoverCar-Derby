using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer;

public class ScoreDisplayView : MonoBehaviour, IScoreDisplayView
{
    [SerializeField] private LeaderboardEntity leaderboardEntityPrefab;
    [SerializeField] private Transform container;
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
        presenter = new ScoreDisplayPresenter(this, scoreManager, this);
        presenter.Initialize();
    }

    private RectTransform GetPanelRect()
    {
        if (panelToMove != null)
            return panelToMove;
        return GetComponent<RectTransform>();
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

    public void MoveToCenter()
    {
        RectTransform rect = GetPanelRect();
        if (rect == null)
            return;

        if (!gamePositionSaved)
        {
            savedAnchorMin = rect.anchorMin;
            savedAnchorMax = rect.anchorMax;
            savedAnchoredPosition = rect.anchoredPosition;
            gamePositionSaved = true;
        }

        SortEntriesByScoreDescending();

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = Vector2.zero;
        rect.pivot = new Vector2(0.5f, 0.5f);
    }

    public void ResetToGamePosition()
    {
        if (!gamePositionSaved)
            return;

        RectTransform rect = GetPanelRect();
        if (rect != null)
        {
            rect.anchorMin = savedAnchorMin;
            rect.anchorMax = savedAnchorMax;
            rect.anchoredPosition = savedAnchoredPosition;
        }
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
