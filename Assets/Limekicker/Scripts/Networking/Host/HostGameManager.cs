using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Authentication;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Host session bootstrap for client-hosted games.
///
/// 1. Registers the host user data with the connection approval server.
/// 2. Creates a public Relay session; the SDK starts NetworkManager as host and returns the join code.
/// 3. Loads PlayScene for all connected clients.
///
/// Called from MainMenuPresenter via NetworkSession when the user starts a host session.
/// </summary>
public class HostGameManager : IDisposable
{
    public string joinCode { get; private set; }
    private readonly NetworkObject playerPrefab;
    public NetworkServer NetworkServer { get; private set; }
    private readonly GameSessionService sessionService = new GameSessionService();
    private const int MaxConnections = 8;
    private const string GameSceneName = "PlayScene";

    public HostGameManager(NetworkObject playerPrefab)
    {
        this.playerPrefab = playerPrefab;
    }

    /// <summary>Creates the session (which starts the host) and loads the game scene.</summary>
    public async Task StartHostAsync()
    {
        NetworkServer = new NetworkServer(NetworkManager.Singleton, playerPrefab);

        UserData hostUserData = new UserData
        {
            userName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "!!Missing Name!!"),
            userAuthId = AuthenticationService.Instance.PlayerId
        };

        // Register host manually so GetUserData works later
        NetworkServer.RegisterHostUserData(hostUserData);

        try
        {
            string playerName = PlayerPrefs.GetString(NameSelector.PlayerNameKey, "Unknown");
            ISession session = await sessionService.CreateHostSessionAsync($"{playerName}'s Lobby", MaxConnections);
            joinCode = session.Code;
        }
        catch (Exception e)
        {
            SessionNotifications.Error(
                "Could not create session.",
                $"Failed to start host: {e.Message}");
            NetworkServer.Dispose();
            NetworkServer = null;
            return;
        }

        JoinCodeHandoff.Publish(joinCode);

        if (NetworkManager.Singleton.IsServer)
        {
            NetworkManager.Singleton.SceneManager.LoadScene(GameSceneName, LoadSceneMode.Single);
        }
    }

    /// <summary>Stops joining once the match has started.</summary>
    public Task LockSessionAsync()
    {
        return sessionService.LockAsync();
    }

    /// <summary>Cleans up session and network resources.</summary>
    public void Dispose()
    {
        Shutdown();
    }

    /// <summary>Shuts down the network server, then deletes the session.</summary>
    public async void Shutdown()
    {
        JoinCodeHandoff.Clear();

        if (NetworkServer != null)
        {
            NetworkServer.Dispose();
        }

        await sessionService.CloseAsync();
    }
}
