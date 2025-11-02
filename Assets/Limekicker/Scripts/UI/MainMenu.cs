using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private TMP_Text queueStatusText;
    [SerializeField] private TMP_Text queueTimerText;
    [SerializeField] private TMP_Text findMatchButtonText;
    [SerializeField] private TMP_InputField joinCodeField;

    private bool isMatchmaking;
    private bool isCanceling;
    private bool isBusy;

    float timeInQueue = 0;

    private void Start()
    {
        if (ClientSingleton.Instance == null)
            return;

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        queueStatusText.text = string.Empty;
        queueTimerText.text = string.Empty;
    }

    private void Update()
    {
        if (isMatchmaking)
        {
            timeInQueue += Time.deltaTime;
            TimeSpan timeSpan = TimeSpan.FromSeconds(timeInQueue);
            queueTimerText.text = string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
        }
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
            isBusy = false;

            findMatchButtonText.text = "Find Match";
            queueStatusText.text = string.Empty;
            queueTimerText.text = string.Empty;

            return;
        }

        if (isBusy)
            return;

        ClientSingleton.Instance.GameManager.MatchmakeAsync(OnMatchMade);

        findMatchButtonText.text = "Cancel";
        queueStatusText.text = "Searching...";
        timeInQueue = 0;
        isMatchmaking = true;
        isBusy = true;
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
        if (isBusy) { return; }

        isBusy = true;

        await HostSingleton.Instance.GameManager.StartHostAsync();

        isBusy = false;
    }

    public async void StartClient()
    {
        if (isBusy) { return; }

        isBusy = true;

        string joinCode = joinCodeField.text.ToUpper();

        await ClientSingleton.Instance.GameManager.StartClientAsync(joinCode);

        isBusy = false;
    }

    public async void JoinASync(Lobby lobby)
    {
        if (isBusy)
            return;

        isBusy = true;

        try
        {
            Lobby joiningLobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobby.Id);

            string joinCode = joiningLobby.Data["JoinCode"].Value;

            await ClientSingleton.Instance.GameManager.StartClientAsync(joinCode);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby: {e.Message}");
        }

        isBusy = false;
    }
}
