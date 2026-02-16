using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;
using VContainer;
using VContainer.Unity;

public class ScoreManager : IScoreManager, IDisposable, IStartable
{
    public Dictionary<PlayerData, int> PlayerScores { get; private set; } = new Dictionary<PlayerData, int>();

    public event Action<PlayerData> OnScoreChanged;
    public event Action<PlayerData> OnPlayerAdded; // for UI

    // SOAP: Track IntVariable SOs per player for score values
    private Dictionary<ulong, IntVariable> playerScoreVariables = new Dictionary<ulong, IntVariable>();
    private Dictionary<ulong, PlayerData> playerDataByClientId = new Dictionary<ulong, PlayerData>();

    private EventBinding<PlayerSpawnedEvent> playerSpawnedEvent;

    [Inject]
    public void Construct()
    {
        playerSpawnedEvent = new EventBinding<PlayerSpawnedEvent>(AddPlayerToScoreBoard);
    }

    public IntVariable GetPlayerScoreVariable(ulong clientId)
    {
        return playerScoreVariables.TryGetValue(clientId, out var variable) ? variable : null;
    }

    public void Start()
    {
        EventBus<PlayerSpawnedEvent>.Register(playerSpawnedEvent);
    }

    private void AddPlayerToScoreBoard(PlayerSpawnedEvent playerSpawnedEvent)
    {
        var data = playerSpawnedEvent.UserData;
        var @object = playerSpawnedEvent.NetworkObject;

        // Check if this is a bot - bots use NetworkObjectId, real players use OwnerClientId
        bool isBot = @object.TryGetComponent<PlayerController>(out var controller) && controller.IsBot;
        ulong clientId = isBot ? @object.NetworkObjectId : @object.OwnerClientId;

        PlayerData playerData = new PlayerData
        {
            PlayerName = data?.userName ?? (isBot && controller != null ? controller.PlayerName.Value : new FixedString32Bytes("Unknown")),
            ClientId = clientId,
            Points = 0
        };

        // SOAP: Create IntVariable SO for this player's score
        IntVariable scoreVariable = ScriptableObject.CreateInstance<IntVariable>();
        scoreVariable.Value = 0;
        playerScoreVariables[clientId] = scoreVariable;
        playerDataByClientId[clientId] = playerData;

        OnPlayerAdded?.Invoke(playerData);
    }

    public void IncreaseScore(PlayerController data, int scoreToAdd)
    {
        // Bots use NetworkObjectId, real players use OwnerClientId (matching collision system)
        ulong clientId = data.IsBot ? data.NetworkObjectId : data.OwnerClientId;
        
        // SOAP: Update IntVariable SO (this will trigger OnValueChanged for UI)
        if (playerScoreVariables.TryGetValue(clientId, out var scoreVariable))
        {
            scoreVariable.Value += scoreToAdd;
            
            // Update PlayerData
            if (playerDataByClientId.TryGetValue(clientId, out var playerData))
            {
                playerData.Points = scoreVariable.Value;
                playerDataByClientId[clientId] = playerData;
            }
            
            OnScoreChanged?.Invoke(playerDataByClientId[clientId]);
        }
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

    public void Dispose()
    {
        EventBus<PlayerSpawnedEvent>.Unregister(playerSpawnedEvent);
    }
}

public struct PlayerData
{
    public FixedString32Bytes PlayerName;
    public ulong ClientId;
    public int Points { get; set; }
}