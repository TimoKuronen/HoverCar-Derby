using TMPro;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public class LeaderboardEntity : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI displayText;
    [SerializeField] private Color myColor;

    public ulong ClientId { get; private set; }
    public int Cash { get; private set; }

    private FixedString32Bytes playerName;

    public void Initialise(ulong clientId, FixedString32Bytes playerName, int cash)
    {
        this.ClientId = clientId;
        this.playerName = playerName;

        if (clientId == NetworkManager.Singleton.LocalClientId)
        {
            displayText.color = myColor;
        }

        UpdateCash(cash);
    }

    public void UpdateCash(int cash)
    {
        Cash = cash;

        UpdateText();
    }

    public void UpdateText()
    {
        displayText.text = $"{transform.GetSiblingIndex() + 1}. {playerName} ({Cash})";
    }
}
