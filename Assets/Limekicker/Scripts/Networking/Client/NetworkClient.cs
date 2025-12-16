using System;
using Unity.Netcode;
using UnityEngine.SceneManagement;

public class NetworkClient : IDisposable
{
    private NetworkManager networkManager;

    private const string MenuSceneName = "MainMenu";

    public NetworkClient(NetworkManager networkManager)
    {
        this.networkManager = networkManager;

        networkManager.OnClientDisconnectCallback += OnClientDisconnect;
    }

    /// <summary>Handles disconnect events. Only processes local client disconnects.</summary>
    private void OnClientDisconnect(ulong clientId)
    {
        if (clientId != 0 && clientId != networkManager.LocalClientId)
        {
            return;
        }

        Disconnect();
    }

    /// <summary>Disconnects client and returns to MainMenu scene.</summary>
    public void Disconnect()
    {
        if (SceneManager.GetActiveScene().name != MenuSceneName)
        {
            SceneManager.LoadScene(MenuSceneName);
        }

        if (networkManager.IsConnectedClient)
        {
            networkManager.Shutdown();
        }
    }

    public void Dispose()
    {
        if (networkManager != null)
        {
            networkManager.OnClientDisconnectCallback -= OnClientDisconnect;
        }
    }
}
