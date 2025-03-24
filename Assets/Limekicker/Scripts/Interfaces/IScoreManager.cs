public interface IScoreManager : IService
{
    void AddPlayer(PlayerData data) { }

    void IncreaseScore(PlayerData data, int scoreToAdd) { }
}
