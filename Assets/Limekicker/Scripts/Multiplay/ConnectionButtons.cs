using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ConnectionButtons : MonoBehaviour
{
    private void Start()
    {
        // Ensure the NetworkManager exists in the scene
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager is not present in the scene. Please add one.");
        }
        else StartHost();
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
        // Check if the player is already connected to a server
        if (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer)
        {
            Debug.Log("Already connected to a server.");
            return;
        }
        // Start the client and connect to the server
        NetworkManager.Singleton.StartClient();
    }
}