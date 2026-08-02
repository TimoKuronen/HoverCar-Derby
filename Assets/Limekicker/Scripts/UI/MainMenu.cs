using System;
using TMPro;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;
using UnityEngine.UI;
using System.Threading.Tasks;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private TMP_Text queueTimerText;
    [SerializeField] private TMP_Text findMatchButtonText;
    [SerializeField] private TMP_InputField joinCodeField;
    [SerializeField] private Toggle spawnBotToggle;
    [SerializeField] private Toggle skipCountdownToggle;

    private bool isMatchmaking;
    private bool isCanceling;
    private bool isBusy;
    private LobbyService lobbyService;

    float timeInQueue = 0;

    private const string SpawnBotKey = "SpawnBotForTesting";
    private const string SkipCountdownKey = "SkipCountdownForTesting";

    private void Start()
    {
        if (!NetworkSession.IsClientInitialized)
            return;

        lobbyService = new LobbyService(this);

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        if (queueTimerText != null)
            queueTimerText.text = string.Empty;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        bool spawnBot = PlayerPrefs.GetInt(SpawnBotKey, 0) == 1;
        if (spawnBotToggle != null)
        {
            spawnBotToggle.isOn = spawnBot;
            spawnBotToggle.onValueChanged.AddListener(OnSpawnBotToggleChanged);
        }

        bool ignoreCountdown = PlayerPrefs.GetInt(SkipCountdownKey, 0) == 1;
        if (skipCountdownToggle != null)
        {
            skipCountdownToggle.isOn = ignoreCountdown;
            skipCountdownToggle.onValueChanged.AddListener(OnSkipCountdownToggleChanged);
        }
#else
        if (spawnBotToggle != null)
            spawnBotToggle.gameObject.SetActive(false);
        if (skipCountdownToggle != null)
            skipCountdownToggle.gameObject.SetActive(false);
#endif
    }

    private void Update()
    {
        if (isMatchmaking && queueTimerText != null)
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
            SessionNotifications.Info("Canceling matchmaking...");
            isCanceling = true;

            await NetworkSession.CancelMatchmakingAsync();

            isCanceling = false;
            isMatchmaking = false;
            isBusy = false;

            findMatchButtonText.text = "Find Match";
            if (queueTimerText != null)
                queueTimerText.text = string.Empty;

            return;
        }

        if (isBusy)
            return;

        NetworkSession.FindMatchAsync(OnMatchMade);

        findMatchButtonText.text = "Cancel";
        SessionNotifications.Info("Searching for a match...");
        timeInQueue = 0;
        isMatchmaking = true;
        isBusy = true;
    }

    private void OnMatchMade(MatchmakerPollingResult result)
    {
        switch (result)
        {
            case MatchmakerPollingResult.Success:
                SessionNotifications.Info("Match found. Joining...");
                break;
            case MatchmakerPollingResult.TicketCreationError:
                SessionNotifications.Error("Matchmaking failed. Please try again.");
                isMatchmaking = false;
                findMatchButtonText.text = "Find Match";
                break;
            case MatchmakerPollingResult.TicketCancellationError:
                SessionNotifications.Info("Matchmaking canceled.");
                isMatchmaking = false;
                findMatchButtonText.text = "Find Match";
                break;
            default:
                SessionNotifications.Error("Matchmaking timed out.");
                isMatchmaking = false;
                findMatchButtonText.text = "Find Match";
                break;
        }
    }

    public async void StartHost()
    {
        if (isBusy)
            return;

        isBusy = true;
        SessionNotifications.Info("Starting host...");

        await NetworkSession.StartHostAsync();

        isBusy = false;
    }

    public async void StartClient()
    {
        if (isBusy)
            return;

        isBusy = true;

        string joinCode = joinCodeField.text.Trim().ToUpper();

        if (string.IsNullOrEmpty(joinCode))
        {
            SessionNotifications.Info("Quick joining first available lobby...");
            await QuickJoinFirstLobby();
        }
        else
        {
            SessionNotifications.Info("Joining game...");
            await NetworkSession.StartClientViaJoinCodeAsync(joinCode);
        }

        isBusy = false;
    }

    public async Task QuickJoinFirstLobby()
    {
        try
        {
            QueryResponse lobbies = await NetworkSession.QueryAvailableLobbiesAsync(count: 1);

            if (lobbies.Results != null && lobbies.Results.Count > 0)
            {
                Lobby firstLobby = lobbies.Results[0];
                SessionNotifications.Info($"Joining lobby: {firstLobby.Name}");
                await NetworkSession.JoinLobbyByIdAsync(firstLobby.Id);
            }
            else
            {
                SessionNotifications.Warn("No lobbies available. Enter a join code from the host.");
            }
        }
        catch (Exception e)
        {
            SessionNotifications.Error(
                "Could not join a lobby.",
                $"Failed to quick join lobby: {e.Message}");
        }
    }

    public async void JoinASync(Lobby lobby)
    {
        await JoinLobbyAsync(lobby);
    }

    private async Task JoinLobbyAsync(Lobby lobby)
    {
        if (isBusy)
            return;

        isBusy = true;

        try
        {
            string joinCode = lobby.Data != null && lobby.Data.ContainsKey("JoinCode")
                ? lobby.Data["JoinCode"].Value
                : null;

            if (string.IsNullOrEmpty(joinCode))
            {
                Lobby joiningLobby = await lobbyService.JoinLobbyByIdAsync(lobby.Id);
                joinCode = joiningLobby.Data["JoinCode"].Value;
            }

            SessionNotifications.Info("Joining game...");
            await NetworkSession.StartClientViaJoinCodeAsync(joinCode);
        }
        catch (LobbyServiceException e)
        {
            SessionNotifications.Error(
                "Could not join that lobby.",
                $"Failed to join lobby: {e.Message}");
        }

        isBusy = false;
    }

    #region Development Testing Methods
    private void OnSpawnBotToggleChanged(bool value)
    {
        PlayerPrefs.SetInt(SpawnBotKey, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    private void OnSkipCountdownToggleChanged(bool value)
    {
        PlayerPrefs.SetInt(SkipCountdownKey, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static bool IsSpawnBotEnabled()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        return PlayerPrefs.GetInt(SpawnBotKey, 0) == 1;
#else
        return false;
#endif
    }

    public static bool IsSkipCountdownEnabled()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        return PlayerPrefs.GetInt(SkipCountdownKey, 0) == 1;
#else
        return false;
#endif
    }
    #endregion

    private void OnDestroy()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (spawnBotToggle != null)
            spawnBotToggle.onValueChanged.RemoveListener(OnSpawnBotToggleChanged);
        if (skipCountdownToggle != null)
            skipCountdownToggle.onValueChanged.RemoveListener(OnSkipCountdownToggleChanged);
#endif
    }
}
