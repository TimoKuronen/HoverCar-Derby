using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class ConnectionButtons : MonoBehaviour
{
    public void StartHost()
    {

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