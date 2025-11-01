using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private TMP_Text queueStatusText;
    [SerializeField] private TMP_Text queueTimerText;
    [SerializeField] private TMP_Text findMatchButtonText;
    [SerializeField] private TMP_InputField joinCodeField;

    private bool isMatchmaking;
    private bool isCanceling;

    private void Start()
    {
        if (ClientSingleton.Instance == null)
            return;

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        queueStatusText.text = string.Empty;
        queueTimerText.text = string.Empty;
    }

    public async void FindMatchButtonPressed()
    {
        if (isCanceling)
            return;

        if (isMatchmaking)
        {
            queueStatusText.text = "Canceling...";
            isCanceling = true;
            await ClientSingleton.Instance.GameManager.CancelMatchmaking();
            isCanceling = false;
            isMatchmaking = false;

            findMatchButtonText.text = "Find Match";
            queueStatusText.text = string.Empty;

            return;
        }

        ClientSingleton.Instance.GameManager.MatchmakeAsync(OnMatchMade);
        findMatchButtonText.text = "Cancel";
        queueStatusText.text = "Searching...";
        isMatchmaking = true;
    }

    private void OnMatchMade(MatchmakerPollingResult result)
    {
        switch (result)
        {
            case MatchmakerPollingResult.Success:
                queueStatusText.text = "Match found! Joining...";
                break;
            case MatchmakerPollingResult.TicketCreationError:
                queueStatusText.text = "Matchmaking failed. Please try again.";
                isMatchmaking = false;
                findMatchButtonText.text = "Find Match";
                break;
            case MatchmakerPollingResult.TicketCancellationError:
                queueStatusText.text = "Matchmaking canceled.";
                isMatchmaking = false;
                findMatchButtonText.text = "Find Match";
                break;
            default:
                queueStatusText.text = "Unknown matchmaking result.";
                isMatchmaking = false;
                findMatchButtonText.text = "Find Match";
                break;
        }
    }

    public async void StartHost()
    {
        await HostSingleton.Instance.GameManager.StartHostAsync();
    }

    public async void StartClient()
    {
        string joinCode = joinCodeField.text.ToUpper();

        await ClientSingleton.Instance.GameManager.StartClientAsync(joinCode);
    }
}
