using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;

/// <summary>
/// Access to the UnityTransport on the scene NetworkManager. Relay setup itself is done by the session SDK.
/// </summary>
public static class ConnectionService
{
    /// <summary>
    /// Gets the UnityTransport component from NetworkManager.Singleton.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown if NetworkManager or UnityTransport is missing</exception>
    public static UnityTransport GetTransport()
    {
        NetworkManager networkManager = NetworkManager.Singleton;
        if (networkManager == null)
        {
            throw new InvalidOperationException("NetworkManager.Singleton is null. Ensure NetworkManager exists in the scene.");
        }

        UnityTransport transport = networkManager.GetComponent<UnityTransport>();
        if (transport == null)
        {
            throw new InvalidOperationException("NetworkManager is missing UnityTransport component. Add UnityTransport to the NetworkManager GameObject.");
        }

        return transport;
    }
}
