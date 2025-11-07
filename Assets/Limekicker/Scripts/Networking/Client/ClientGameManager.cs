using System;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

public class ClientGameManager : IDisposable
{
    private JoinAllocation allocation;
    private const string MenuSceneName = "MainMenu";
    private const string PlaySceneName = "PlayScene";

    private NetworkClient networkClient;
    private MatchplayMatchmaker matchmaker;

    private UserData userData;

    /// <summary>Initializes Unity Services, authenticates user, and creates UserData.</summary>
    public async Task<bool> InitAsync()
    {
        await UnityServices.InitializeAsync();

        networkClient = new NetworkClient(NetworkManager.Singleton);
        matchmaker = new MatchplayMatchmaker();

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
    public void StartClient(string ip, int port)
    {
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, (ushort)port);

        ConnectClient();
    }

    /// <summary>Connects client via Relay join code (used for lobby-based joining).</summary>
    public async Task StartClientAsync(string joinCode)
    {
        try
        {
            allocation = await Relay.Instance.JoinAllocationAsync(joinCode);
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to start client: {e.Message}");
            return;
        }

        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();

        // Use "dtls" for more security but if facing issues, try "udp"
        RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
        transport.SetRelayServerData(relayServerData);

        ConnectClient();
    }

    /// <summary>Sets up connection callbacks and starts client with UserData payload.</summary>
    private void ConnectClient()
    {
        // Attach diagnostics and scene sync guards
        var nm = NetworkManager.Singleton;
        if (nm.SceneManager != null)
        {
            nm.SceneManager.OnSceneEvent -= HandleSceneEvent;
            nm.SceneManager.OnSceneEvent += HandleSceneEvent;
        }
        nm.OnClientConnectedCallback -= HandleClientConnected;
        nm.OnClientConnectedCallback += HandleClientConnected;

        string payload = JsonUtility.ToJson(userData);
        byte[] payloadBytes = Encoding.UTF8.GetBytes(payload);

        NetworkManager.Singleton.NetworkConfig.ConnectionData = payloadBytes;
        NetworkManager.Singleton.StartClient();
    }

    /// <summary>Fallback scene loading if Netcode scene management is disabled.</summary>
    private void HandleClientConnected(ulong clientId)
    {
        // If scene management is off, clients won't auto-switch to the server scene
        if (!NetworkManager.Singleton.NetworkConfig.EnableSceneManagement)
        {
            Debug.LogWarning("[Client] Enable Scene Management is OFF; loading PlayScene locally as fallback.");
            if (SceneManager.GetActiveScene().name != PlaySceneName)
            {
                SceneManager.LoadScene(PlaySceneName);
            }
        }
    }

    /// <summary>Logs scene events for debugging.</summary>
    private void HandleSceneEvent(SceneEvent sceneEvent)
    {
        Debug.Log($"[Client] SceneEvent: {sceneEvent.SceneEventType} -> {sceneEvent.SceneName} (client={sceneEvent.ClientId})");
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
        networkClient?.Dispose();
    }
}
