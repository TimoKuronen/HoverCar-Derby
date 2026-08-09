public interface IScoreDisplayView
{
    void AddPlayer(ulong clientId, string playerName, int initialScore);
    void UpdatePlayerScore(ulong clientId, int newScore);
    void ResetToGamePosition();
}
