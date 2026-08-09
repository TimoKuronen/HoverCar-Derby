using System.Collections.Generic;
using UnityEngine;

public class ScoreDisplayView : MonoBehaviour, IScoreDisplayView
{
    [SerializeField] private LeaderboardEntity leaderboardEntityPrefab;
    [SerializeField] private Transform scoreContainer;
    [SerializeField] private RectTransform panelToMove;

    private Dictionary<ulong, LeaderboardEntity> playerScores = new Dictionary<ulong, LeaderboardEntity>();

    private bool gamePositionSaved;
    private Vector2 savedAnchorMin;
    private Vector2 savedAnchorMax;
    private Vector2 savedAnchoredPosition;

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
            entity.UpdatePoints(newScore);
    }

    public void ResetToGamePosition()
    {
        if (!gamePositionSaved)
            return;

        panelToMove.anchorMin = savedAnchorMin;
        panelToMove.anchorMax = savedAnchorMax;
        panelToMove.anchoredPosition = savedAnchoredPosition;
    }

    private void Awake()
    {
        if (panelToMove == null)
            return;

        savedAnchorMin = panelToMove.anchorMin;
        savedAnchorMax = panelToMove.anchorMax;
        savedAnchoredPosition = panelToMove.anchoredPosition;
        gamePositionSaved = true;
    }
}
