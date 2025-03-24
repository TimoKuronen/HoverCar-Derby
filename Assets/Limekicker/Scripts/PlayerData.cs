using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public struct PlayerData
{
    public string PlayerName { get; private set; }
    public int Points { get; private set; }

    public void AddPoints(int points)
    {
        this.Points += points;
    }

    public void SetPlayerName(string playerName)
    {
        this.PlayerName = playerName;
    }
}
