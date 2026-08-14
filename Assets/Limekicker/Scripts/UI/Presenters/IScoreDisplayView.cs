/// <summary>
/// Contract for per-player score rows and score updates.
/// </summary>
public interface IScoreDisplayView
{
    void AddPlayer(ulong clientId, string playerName, int initialScore);
    void UpdatePlayerScore(ulong clientId, int newScore);
    void RemovePlayer(ulong clientId);
    void ResetToGamePosition();
}
