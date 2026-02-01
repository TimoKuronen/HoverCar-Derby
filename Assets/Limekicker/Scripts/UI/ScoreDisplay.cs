using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class ScoreDisplay : MonoBehaviour
{
    [SerializeField] private LeaderboardEntity leaderboardEntityPrefab;
    [SerializeField] private Transform container;

    private IScoreManager scoreManager;
    private Dictionary<ulong, LeaderboardEntity> playerScores = new Dictionary<ulong, LeaderboardEntity>();

    [Inject]
    public void Construct(IScoreManager scoreManager)
    {
        this.scoreManager = scoreManager;
        this.scoreManager.OnPlayerAdded += AddPlayer;
        this.scoreManager.OnScoreChanged += UpdateScore;
    }

    private void AddPlayer(PlayerData data)
    {
        Debug.Log($"Adding player {data.ClientId} with score {data.Points}");
        
        // Check if player already exists to prevent duplicate key errors
        if (playerScores.ContainsKey(data.ClientId))
        {
            Debug.LogWarning($"Player {data.ClientId} already exists in score display, updating instead of adding.");
            playerScores[data.ClientId].Initialise(data.ClientId, data.PlayerName.Value.ToString(), data.Points);
            return;
        }
        
        playerScores.Add(data.ClientId, Instantiate(leaderboardEntityPrefab, container));
        playerScores[data.ClientId].Initialise(data.ClientId, data.PlayerName.Value.ToString(), data.Points);
    }

    private void UpdateScore(PlayerData data)
    {
        playerScores[data.ClientId].UpdatePoints(data.Points);
    }

    private void OnDestroy()
    {
        if (scoreManager != null)
        {
            scoreManager.OnPlayerAdded -= AddPlayer;
            scoreManager.OnScoreChanged -= UpdateScore;
        }
    }
}