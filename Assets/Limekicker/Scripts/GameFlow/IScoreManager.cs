using System;
using System.Collections.Generic;

/// <summary>
/// Contract for score tracking and ranking.
/// </summary>
public interface IScoreManager
{
    event Action<PlayerData> OnScoreChanged;
    event Action<PlayerData> OnPlayerAdded;

    PlayerData GetLeadingPlayer();
    IReadOnlyList<PlayerData> GetRankedPlayersByScore();
}
