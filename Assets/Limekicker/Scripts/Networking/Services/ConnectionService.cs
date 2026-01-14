using System;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// Centralized service for UnityTransport configuration.
/// Handles all transport setup including Relay and direct IP/port connections.
/// </summary>
public static class ConnectionService
{
    private const string RelayProtocol = "dtls"; // Use dtls for secure, encrypted connections

    /// <summary>
    /// Gets the UnityTransport component from NetworkManager.Singleton.
    /// </summary>
    /// <returns>The UnityTransport component</returns>
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

    /// <summary>
    /// Configures the UnityTransport with Relay server data for hosting.
    /// </summary>
    /// <param name="allocation">The allocation to configure transport with</param>
    /// <exception cref="ArgumentNullException">Thrown if allocation is null</exception>
    /// <exception cref="InvalidOperationException">Thrown if NetworkManager or UnityTransport is missing</exception>
    public static void ConfigureRelayForHost(Allocation allocation)
    {
        if (allocation == null)
        {
            throw new ArgumentNullException(nameof(allocation));
        }

        UnityTransport transport = GetTransport();

        try
        {
            RelayServerData relayServerData = new RelayServerData(allocation, RelayProtocol);
            transport.SetRelayServerData(relayServerData);
            Debug.Log($"[ConnectionService] Transport configured for host with Relay protocol: {RelayProtocol}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConnectionService] Failed to configure Relay transport for host: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// Configures the UnityTransport with Relay server data for joining.
    /// </summary>
    /// <param name="allocation">The join allocation to configure transport with</param>
    /// <exception cref="ArgumentNullException">Thrown if allocation is null</exception>
    /// <exception cref="InvalidOperationException">Thrown if NetworkManager or UnityTransport is missing</exception>
    public static void ConfigureRelayForClient(JoinAllocation allocation)
    {
        if (allocation == null)
        {
            throw new ArgumentNullException(nameof(allocation));
        }

        UnityTransport transport = GetTransport();

        try
        {
            RelayServerData relayServerData = new RelayServerData(allocation, RelayProtocol);
            transport.SetRelayServerData(relayServerData);
            Debug.Log($"[ConnectionService] Transport configured for client with Relay protocol: {RelayProtocol}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConnectionService] Failed to configure Relay transport for client: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// Configures the UnityTransport for direct IP/port connection (used for matchmaking).
    /// </summary>
    /// <param name="ip">The server IP address</param>
    /// <param name="port">The server port</param>
    /// <exception cref="ArgumentException">Thrown if IP is null or empty</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if port is out of valid range</exception>
    /// <exception cref="InvalidOperationException">Thrown if NetworkManager or UnityTransport is missing</exception>
    public static void ConfigureDirectConnection(string ip, int port)
    {
        if (string.IsNullOrWhiteSpace(ip))
        {
            throw new ArgumentException("IP address cannot be null or empty", nameof(ip));
        }

        if (port < 1 || port > ushort.MaxValue)
        {
            throw new ArgumentOutOfRangeException(nameof(port), $"Port must be between 1 and {ushort.MaxValue}");
        }

        UnityTransport transport = GetTransport();

        try
        {
            transport.SetConnectionData(ip, (ushort)port);
            Debug.Log($"[ConnectionService] Transport configured for direct connection: {ip}:{port}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConnectionService] Failed to configure direct connection: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// Configures the UnityTransport for server binding (dedicated server).
    /// In UNITY_SERVER builds, binds to Multiplay allocated port.
    /// </summary>
    /// <param name="ip">The IP address to bind to (ignored in UNITY_SERVER builds)</param>
    /// <param name="port">The port to bind to (ignored in UNITY_SERVER builds)</param>
    /// <exception cref="InvalidOperationException">Thrown if NetworkManager or UnityTransport is missing</exception>
    public static void ConfigureServerBinding(string ip, int port)
    {
        UnityTransport transport = GetTransport();

        try
        {
#if UNITY_SERVER
            var config = Unity.Services.Multiplay.MultiplayService.Instance.ServerConfig;
            transport.SetConnectionData("0.0.0.0", (ushort)config.Port);
            Debug.Log($"[ConnectionService] Transport configured for server binding (Multiplay): 0.0.0.0:{config.Port}");
#else
            transport.SetConnectionData(ip, (ushort)port);
            Debug.Log($"[ConnectionService] Transport configured for server binding: {ip}:{port}");
#endif
        }
        catch (Exception e)
        {
            Debug.LogError($"[ConnectionService] Failed to configure server binding: {e.Message}");
            throw;
        }
    }
}


