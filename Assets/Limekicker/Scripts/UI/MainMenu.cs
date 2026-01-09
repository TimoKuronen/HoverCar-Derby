using System;
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
    [SerializeField] private UnityEngine.UI.Toggle spawnBotToggle;

    private bool isMatchmaking;
    private bool isCanceling;
    private bool isBusy;
    private LobbyService lobbyService;

    float timeInQueue = 0;

    private const string LastJoinCodeKey = "LastJoinCode";
    private const string SpawnBotKey = "SpawnBotForTesting";

    private void Start()
    {
        if (!NetworkSession.IsClientInitialized)
            return;

        lobbyService = new LobbyService(this);

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        queueStatusText.text = string.Empty;
        queueTimerText.text = string.Empty;

        // Auto-fill last used join code for easier testing
        string lastJoinCode = PlayerPrefs.GetString(LastJoinCodeKey, "");
        if (!string.IsNullOrEmpty(lastJoinCode) && joinCodeField != null)
        {
            joinCodeField.text = lastJoinCode;
        }

        // Load bot spawn toggle state
        if (spawnBotToggle != null)
        {
            bool spawnBot = PlayerPrefs.GetInt(SpawnBotKey, 0) == 1;
            spawnBotToggle.isOn = spawnBot;
            spawnBotToggle.onValueChanged.AddListener(OnSpawnBotToggleChanged);
        }
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

            await NetworkSession.CancelMatchmakingAsync();

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

        NetworkSession.FindMatchAsync(OnMatchMade);

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
                queueStatusText.text = "Timeout Error.";
                isMatchmaking = false;
                findMatchButtonText.text = "Find Match";
                break;
        }
    }

    public async void StartHost()
    {
        if (isBusy) { return; }

        isBusy = true;

        await NetworkSession.StartHostAsync();

        isBusy = false;
    }

    public async void StartClient()
    {
        if (isBusy) { return; }

        isBusy = true;

        string joinCode = joinCodeField.text.Trim().ToUpper();

        // If join code is empty, try to quick join the first available lobby
        if (string.IsNullOrEmpty(joinCode))
        {
            await QuickJoinFirstLobby();
        }
        else
        {
            // Save join code for next time
            PlayerPrefs.SetString(LastJoinCodeKey, joinCode);
            PlayerPrefs.Save();

            await NetworkSession.StartClientViaJoinCodeAsync(joinCode);
        }

        isBusy = false;
    }

    /// <summary>Quick joins the first available lobby without needing a join code.</summary>
    public async System.Threading.Tasks.Task QuickJoinFirstLobby()
    {
        try
        {
            QueryResponse lobbies = await NetworkSession.QueryAvailableLobbiesAsync(count: 1);

            if (lobbies.Results != null && lobbies.Results.Count > 0)
            {
                Lobby firstLobby = lobbies.Results[0];
                Debug.Log($"Quick joining lobby: {firstLobby.Name}");
                await NetworkSession.JoinLobbyByIdAsync(firstLobby.Id);
            }
            else
            {
                Debug.LogWarning("No available lobbies found for quick join. Please enter a join code or wait for a lobby to be created.");
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to quick join lobby: {e.Message}");
        }
    }

    public async void JoinASync(Lobby lobby)
    {
        await JoinLobbyAsync(lobby);
    }

    /// <summary>Internal method to join a lobby (returns Task for awaitable operations).</summary>
    private async System.Threading.Tasks.Task JoinLobbyAsync(Lobby lobby)
    {
        if (isBusy)
            return;

        isBusy = true;

        try
        {
            // Get join code from lobby before joining
            string joinCode = lobby.Data != null && lobby.Data.ContainsKey("JoinCode") 
                ? lobby.Data["JoinCode"].Value 
                : null;

            if (string.IsNullOrEmpty(joinCode))
            {
                // If join code not in lobby data, join lobby first to get it
                Lobby joiningLobby = await lobbyService.JoinLobbyByIdAsync(lobby.Id);
                joinCode = joiningLobby.Data["JoinCode"].Value;
            }

            // Save join code for next time
            PlayerPrefs.SetString(LastJoinCodeKey, joinCode);
            PlayerPrefs.Save();

            // Update the input field with the join code
            if (joinCodeField != null)
            {
                joinCodeField.text = joinCode;
            }

            await NetworkSession.StartClientViaJoinCodeAsync(joinCode);
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"Failed to join lobby: {e.Message}");
        }

        isBusy = false;
    }

    private void OnSpawnBotToggleChanged(bool value)
    {
        PlayerPrefs.SetInt(SpawnBotKey, value ? 1 : 0);
        PlayerPrefs.Save();
        Debug.Log($"[MainMenu] Spawn bot toggle changed to: {value}");
    }

    /// <summary>Gets whether bot spawning is enabled for testing.</summary>
    public static bool IsSpawnBotEnabled()
    {
        return PlayerPrefs.GetInt(SpawnBotKey, 0) == 1;
    }
}
