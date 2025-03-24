using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScoreManager : IScoreManager
{
    public Dictionary<PlayerData, int> PlayerScores { get; private set; }  = new Dictionary<PlayerData, int>();

    public void Initialize() { }

    public void AddPlayer(PlayerData data)
    {
        PlayerScores.Add(data, 0);
    }

    public void IncreaseScore(PlayerData data, int scoreToAdd)
    {
        PlayerScores[data] += scoreToAdd;
    }
}
