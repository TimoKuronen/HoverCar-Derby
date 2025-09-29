using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Collections;
using UnityEngine;

public class LeaderboardEntity : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI displayText;

    public ulong ClientId { get; private set; }
    public int Cash { get; private set; }
    private FixedString32Bytes playerName;

    public void Initialise(ulong clientId, FixedString32Bytes playerName, int cash)
    {
        this.ClientId = clientId;
        this.playerName = playerName;
        Cash = cash;

        UpdateCash(cash);
    }

    public void UpdateCash(int cash)
    {
        Cash = cash;

        UpdateText();
    }

    private void UpdateText()
    {
        displayText.text = $"1. {playerName} ({Cash})";
    }
}
