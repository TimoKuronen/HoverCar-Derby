using System;
using System.Collections.Generic;

public interface IScoreManager
{
    void IncreaseScore(PlayerData data, int scoreToAdd) { }

    event Action<PlayerData> OnScoreChanged;
    event Action<PlayerData> OnPlayerAdded;

    PlayerData GetLeadingPlayer();
    IReadOnlyList<PlayerData> GetRankedPlayersByScore();
    IntVariable GetPlayerScoreVariable(ulong clientId);
}
