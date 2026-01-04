using System.Collections.Generic;

public class ScoreManager : IScoreManager
{
    public Dictionary<PlayerData, int> PlayerScores { get; private set; } = new Dictionary<PlayerData, int>();

    public void Initialize() 
    {
        PlayerController.OnPlayerSpawned += AddPlayer;
    }

    public void AddPlayer(PlayerController data)
    {
        PlayerData playerData = new PlayerData
        {
            PlayerName = data.PlayerName.Value.ToString(),
            Points = 0
        };     
    }

    public void IncreaseScore(PlayerController data, int scoreToAdd)
    {
        
    }
}

public struct PlayerData
{
    public string PlayerName { get; set; }
    public int Points { get; set; }
}