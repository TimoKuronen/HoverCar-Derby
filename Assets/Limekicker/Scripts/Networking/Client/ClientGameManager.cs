using System;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientGameManager : IDisposable
{
    private JoinAllocation allocation;
    private const string MenuSceneName = "MainMenu";
    private const string PlaySceneName = "PlayScene";
    private const int RelayConnectTimeoutMs = 15000;

    private NetworkClient networkClient;
    private MatchplayMatchmaker matchmaker;
    private RelayService relayService;

    private UserData userData;

    /// <summary>Initializes Unity Services, authenticates user, and creates UserData.</summary>
    public async Task<bool> InitAsync()
    {
        await UnityServices.InitializeAsync();

        networkClient = new NetworkClient(NetworkManager.Singleton);
        matchmaker = new MatchplayMatchmaker();
        relayService = new RelayService();

        AuthenticatorState authenticatorState = await AuthenticatorHandler.DoAuthentication();

        if (authenticatorState == AuthenticatorState.Authenticated)
        {
            userData = new UserData
            {
                userName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "!!Missing Name!!"),
                userAuthId = AuthenticationService.Instance.PlayerId
            };

            return true;
        }

        Debug.LogError("Authentication failed. Cannot proceed with ClientGameManager initialization.");
        return false;
    }

    /// <summary>Loads MainMenu scene.</summary>
    public void GoToMenu()
    {
        SceneManager.LoadScene(MenuSceneName);
    }

    /// <summary>Connects client directly to server via IP/port (used for matchmaking).</summary>
    public async void StartClient(string ip, int port)
    {
        ConnectionService.ConfigureDirectConnection(ip, port);
        await ConnectClientAsync();
    }

    /// <summary>Connects client via Relay join code (used for lobby-based joining).</summary>
    public async Task StartClientAsync(string joinCode)
    {
        try
        {
            allocation = await relayService.JoinAllocationAsync(joinCode);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ClientGameManager] Failed to join Relay allocation: {e.Message}");
            return;
        }

        try
        {
            relayService.ConfigureTransportForClient(allocation);
        }
        catch (Exception e)
        {
            Debug.LogError($"[ClientGameManager] Failed to configure transport: {e.Message}");
            return;
        }

        await ConnectClientAsync();
    }

    /// <summary>Sets up connection callbacks and starts client with UserData payload.</summary>
    private async Task ConnectClientAsync()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null)
        {
            Debug.LogError("[ClientGameManager] NetworkManager.Singleton is null.");
            return;
        }

        if (userData == null)
        {
            Debug.LogError("[ClientGameManager] userData is null; authenticate before connecting.");
            return;
        }

        await EnsureNetworkShutdownAsync(nm);

        UnityTransport transport = ConnectionService.GetTransport();
        transport.ConnectTimeoutMS = RelayConnectTimeoutMs;

        if (nm.SceneManager != null)
        {
            nm.SceneManager.OnSceneEvent -= HandleSceneEvent;
            nm.SceneManager.OnSceneEvent += HandleSceneEvent;
        }

        nm.OnClientConnectedCallback -= HandleClientConnected;
        nm.OnClientConnectedCallback += HandleClientConnected;
        nm.OnClientDisconnectCallback -= HandleClientDisconnect;
        nm.OnClientDisconnectCallback += HandleClientDisconnect;

        string payload = JsonUtility.ToJson(userData);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);
        nm.NetworkConfig.ConnectionData = payloadBytes;

        Debug.Log($"[ClientGameManager] Starting client as {userData.userName} ({userData.userAuthId})");

        bool started = nm.StartClient();
        if (!started)
        {
            Debug.LogError($"[ClientGameManager] StartClient returned false. IsListening={nm.IsListening}, IsClient={nm.IsClient}, IsConnectedClient={nm.IsConnectedClient}");
            return;
        }

        Debug.Log("[ClientGameManager] StartClient succeeded; waiting for host connection and scene sync.");
    }

    private static async Task EnsureNetworkShutdownAsync(NetworkManager nm)
    {
        if (!nm.IsListening)
            return;

        Debug.LogWarning("[ClientGameManager] NetworkManager is already listening; shutting down before reconnect.");
        nm.Shutdown();

        float timeoutAt = Time.realtimeSinceStartup + 3f;
        while (nm.IsListening && Time.realtimeSinceStartup < timeoutAt)
        {
            await Task.Yield();
        }

        if (nm.IsListening)
        {
            Debug.LogError("[ClientGameManager] NetworkManager is still listening after Shutdown.");
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (clientId != NetworkManager.Singleton.LocalClientId)
            return;

        Debug.Log($"[ClientGameManager] Connected to host. LocalClientId={clientId}");

        if (!NetworkManager.Singleton.NetworkConfig.EnableSceneManagement)
        {
            Debug.LogWarning("[ClientGameManager] Scene management is off; loading PlayScene locally as fallback.");
            if (SceneManager.GetActiveScene().name != PlaySceneName)
            {
                SceneManager.LoadScene(PlaySceneName);
            }
        }
    }

    private void HandleClientDisconnect(ulong clientId)
    {
        if (clientId != 0 && clientId != NetworkManager.Singleton.LocalClientId)
            return;

        Debug.LogWarning($"[ClientGameManager] Disconnected from host. clientId={clientId}, localClientId={NetworkManager.Singleton.LocalClientId}");
    }

    private void HandleSceneEvent(SceneEvent sceneEvent)
    {
        Debug.Log($"[ClientGameManager] SceneEvent: {sceneEvent.SceneEventType} -> {sceneEvent.SceneName} (client={sceneEvent.ClientId})");
    }

    /// <summary>Starts matchmaking process. Calls callback with result.</summary>
    public async void MatchmakeAsync(Action<MatchmakerPollingResult> onMatchmakeResponse)
    {
        if (matchmaker.IsMatchmaking)
        {
            return;
        }
        MatchmakerPollingResult matchResult = await GetMatchAsync();
        onMatchmakeResponse?.Invoke(matchResult);
    }

    /// <summary>Polls matchmaker for match assignment. Connects to server if found.</summary>
    private async Task<MatchmakerPollingResult> GetMatchAsync()
    {
        MatchmakingResult matchmakingResult = await matchmaker.Matchmake(userData);

        if (matchmakingResult.result == MatchmakerPollingResult.Success)
        {
            StartClient(matchmakingResult.ip, matchmakingResult.port);
            return MatchmakerPollingResult.Success;
        }

        return matchmakingResult.result;
    }

    /// <summary>Cancels active matchmaking ticket.</summary>
    public async Task CancelMatchmaking()
    {
        await matchmaker.CancelMatchmaking();
    }

    /// <summary>Disconnects from server and returns to menu.</summary>
    public void Disconnect()
    {
        networkClient.Disconnect();
    }

    public void Dispose()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleClientDisconnect;
        }

        networkClient?.Dispose();
    }
}
