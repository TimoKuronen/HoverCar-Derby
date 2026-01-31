using System;

public interface IScoreManager
{
    void IncreaseScore(PlayerData data, int scoreToAdd) { }

    event Action<PlayerData> OnScoreChanged;
    event Action<PlayerData> OnPlayerAdded;

    PlayerData GetLeadingPlayer();
}