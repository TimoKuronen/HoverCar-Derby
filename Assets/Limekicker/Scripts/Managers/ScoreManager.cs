using System;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using VContainer;

public class ScoreManager : IScoreManager
{
    public Dictionary<PlayerData, int> PlayerScores { get; private set; } = new Dictionary<PlayerData, int>();

    public event Action<PlayerData> OnScoreChanged;
    public event Action<PlayerData> OnPlayerAdded;

    private List<PlayerData> players = new List<PlayerData>();
    private IPlayerSpawnManager playerSpawnManager;

    [Inject]
    public void Construct(IPlayerSpawnManager playerSpawnManager)
    {
        this.playerSpawnManager = playerSpawnManager;
        this.playerSpawnManager.OnPlayerSpawned += AddPlayer;
    }

    private void AddPlayer(UserData data, NetworkObject @object)
    {
        PlayerData playerData = new PlayerData
        {
            PlayerName = data.userName,
            ClientId = @object.NetworkObjectId,
            Points = 0
        };

        players.Add(playerData);
        OnPlayerAdded?.Invoke(playerData);
    }
    public void IncreaseScore(PlayerController data, int scoreToAdd)
    {
        OnScoreChanged?.Invoke(new PlayerData
        {
            PlayerName = data.PlayerName.Value,
            ClientId = data.OwnerClientId,
            Points = scoreToAdd
        });
    }

    public PlayerData GetLeadingPlayer()
    {
        PlayerData leadingPlayer = new PlayerData();
        int highestScore = -1;

        foreach (var playerScore in PlayerScores)
        {
            if (playerScore.Value > highestScore)
            {
                highestScore = playerScore.Value;
                leadingPlayer = playerScore.Key;
            }
        }

        return leadingPlayer;
    }
}

public struct PlayerData
{
    public FixedString32Bytes PlayerName;
    public ulong ClientId;
    public int Points { get; set; }
}