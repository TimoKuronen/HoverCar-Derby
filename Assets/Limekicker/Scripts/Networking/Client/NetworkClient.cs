using System;
using Unity.Netcode;
using UnityEngine;
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

        Debug.LogWarning($"[NetworkClient] Local client disconnected. clientId={clientId}, connected={networkManager.IsConnectedClient}, listening={networkManager.IsListening}");
        Disconnect();
    }

    /// <summary>Disconnects client and returns to MainMenu scene.</summary>
    public void Disconnect()
    {
        if (networkManager != null && networkManager.IsListening)
        {
            networkManager.Shutdown();
        }

        if (SceneManager.GetActiveScene().name != MenuSceneName)
        {
            SceneManager.LoadScene(MenuSceneName);
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
