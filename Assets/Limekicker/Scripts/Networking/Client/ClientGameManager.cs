using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Client authentication, session join, and scene transition orchestration.
/// </summary>
public class ClientGameManager : IDisposable
{
    private const string MenuSceneName = "MainMenu";
    private const string PlaySceneName = "PlayScene";
    private const int RelayConnectTimeoutMs = 15000;

    private NetworkClient networkClient;
    private readonly GameSessionService sessionService = new GameSessionService();

    private UserData userData;

    /// <summary>Initializes Unity Services, authenticates user, and creates UserData.</summary>
    public async Task<bool> InitAsync()
    {
        await UnityServices.InitializeAsync();

        networkClient = new NetworkClient(NetworkManager.Singleton);

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

        SessionNotifications.Error(
            "Authentication failed. Could not sign in.",
            "Authentication failed. Cannot proceed with ClientGameManager initialization.");
        return false;
    }

    /// <summary>Loads MainMenu scene.</summary>
    public void GoToMenu()
    {
        SceneManager.LoadScene(MenuSceneName);
    }

    /// <summary>Joins the session with the given code. Returns false when the join failed.</summary>
    public Task<bool> StartClientAsync(string sessionCode)
    {
        return JoinAsync(() => sessionService.JoinByCodeAsync(sessionCode), "Could not join. Check the join code.");
    }

    /// <summary>Joins a session found by <see cref="QueryAvailableSessionsAsync"/>. Returns false when the join failed.</summary>
    public Task<bool> StartClientByIdAsync(string sessionId)
    {
        return JoinAsync(() => sessionService.JoinByIdAsync(sessionId), "Could not join this lobby.");
    }

    public Task<IReadOnlyList<ISessionInfo>> QueryAvailableSessionsAsync(int count)
    {
        return sessionService.QueryAvailableAsync(count);
    }

    private async Task<bool> JoinAsync(Func<Task<ISession>> join, string failureMessage)
    {
        NetworkManager nm = await PrepareClientAsync();
        if (nm == null)
            return false;

        try
        {
            SessionNotifications.Info($"Connecting as {userData.userName}...");
            await join();
            return true;
        }
        catch (Exception e)
        {
            SessionNotifications.Error(
                failureMessage,
                $"[ClientGameManager] Failed to join session: {e.Message}");

            if (nm.IsListening)
            {
                nm.Shutdown();
            }

            await sessionService.CloseAsync();
            return false;
        }
    }

    /// <summary>Resets the network, wires connection callbacks and sets the UserData payload. Returns null when not ready to connect.</summary>
    private async Task<NetworkManager> PrepareClientAsync()
    {
        NetworkManager nm = NetworkManager.Singleton;
        if (nm == null)
        {
            SessionNotifications.Error(
                "Network manager is missing.",
                "[ClientGameManager] NetworkManager.Singleton is null.");
            return null;
        }

        if (userData == null)
        {
            SessionNotifications.Error(
                "Not signed in.",
                "[ClientGameManager] userData is null; authenticate before connecting.");
            return null;
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
        nm.NetworkConfig.ConnectionData = Encoding.UTF8.GetBytes(payload);

        return nm;
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
            SessionNotifications.Error(
                "Could not reset network connection.",
                "[ClientGameManager] NetworkManager is still listening after Shutdown.");
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        if (clientId != NetworkManager.Singleton.LocalClientId)
            return;

        SessionNotifications.Info("Connected to host. Loading game...");

        if (!NetworkManager.Singleton.NetworkConfig.EnableSceneManagement)
        {
            Debug.LogWarning("[ClientGameManager] Scene management is off; loading PlayScene locally as fallback.");
            if (SceneManager.GetActiveScene().name != PlaySceneName)
            {
                SceneManager.LoadScene(PlaySceneName);
            }
        }
    }

    private async void HandleClientDisconnect(ulong clientId)
    {
        if (clientId != 0 && clientId != NetworkManager.Singleton.LocalClientId)
            return;

        SessionNotifications.Warn(
            "Disconnected from host.",
            $"[ClientGameManager] Disconnected from host. clientId={clientId}, localClientId={NetworkManager.Singleton.LocalClientId}");

        await sessionService.CloseAsync();
    }

    private void HandleSceneEvent(SceneEvent sceneEvent)
    {
        if (sceneEvent.SceneEventType == SceneEventType.LoadComplete && sceneEvent.ClientId == NetworkManager.Singleton.LocalClientId)
            SessionNotifications.Info("Game loaded.");
    }

    /// <summary>Disconnects from the host, returns to menu and leaves the session.</summary>
    public async void Disconnect()
    {
        networkClient.Disconnect();
        await sessionService.CloseAsync();
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
