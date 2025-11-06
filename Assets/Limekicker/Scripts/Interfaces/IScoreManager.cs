public interface IScoreManager
{
    void AddPlayer(PlayerData data) { }

    void IncreaseScore(PlayerData data, int scoreToAdd) { }
}
