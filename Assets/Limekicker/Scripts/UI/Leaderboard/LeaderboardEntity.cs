using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

/// <summary>
/// UI row that displays one player's leaderboard name and score.
/// </summary>
public class LeaderboardEntity : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI displayText;
    [SerializeField] private Color myColor;

    public ulong ClientId { get; private set; }
    public int Points { get; private set; }

    private FixedString32Bytes playerName;

    public void Initialise(ulong clientId, FixedString32Bytes playerName, int points)
    {
        this.ClientId = clientId;
        this.playerName = playerName;

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            displayText.color = myColor;
        }

        UpdatePoints(points);
    }

    public void Initialise(ulong clientId, string playerName, int points)
    {
        Initialise(clientId, new FixedString32Bytes(playerName), points);
    }

    public void UpdatePoints(int points)
    {
        Points = points;

        UpdateText();
    }

    public void UpdateText()
    {
        displayText.text = $"{transform.GetSiblingIndex() + 1}. {playerName} ({Points})";
    }
}
