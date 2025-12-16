using Unity.Netcode;
using UnityEngine;

/// <summary>
/// Legacy helper for directly starting a Netcode host/client from UI buttons.
///
/// This bypasses the newer flow that uses <see cref="ApplicationController"/>,
/// Relay, Lobbies, authentication, etc. It is safe to keep around for quick
/// local testing, but current menus (MainMenu) do not depend on it.
/// </summary>
public class ConnectionButtons : MonoBehaviour
{
    private void Start()
    {
        // Ensure the NetworkManager exists in the scene
        //if (NetworkManager.Singleton == null)
        //{
        //    Debug.LogError("NetworkManager is not present in the scene. Please add one.");
        //}
    }

    public void StartHost()
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            Debug.Log("Already connected to a server.");
            return;
        }

        NetworkManager.Singleton.StartHost();
    }
    public void StartClient()
    {
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            Debug.Log("Already connected to a server.");
            return;
        }

        NetworkManager.Singleton.StartClient();
    }
}