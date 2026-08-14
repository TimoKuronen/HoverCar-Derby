using System;
using System.Collections.Generic;

/// <summary>
/// Contract for score tracking, ranking, and per-player score variables.
/// </summary>
public interface IScoreManager
{
    void IncreaseScore(PlayerData data, int scoreToAdd) { }

    event Action<PlayerData> OnScoreChanged;
    event Action<PlayerData> OnPlayerAdded;

    PlayerData GetLeadingPlayer();
    IReadOnlyList<PlayerData> GetRankedPlayersByScore();
    IntVariable GetPlayerScoreVariable(ulong clientId);
}
