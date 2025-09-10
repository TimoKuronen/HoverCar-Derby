using Unity.Netcode;
using UnityEngine;

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