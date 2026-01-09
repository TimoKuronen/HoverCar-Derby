using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

/// <summary>
/// Centralized service for Unity Relay operations.
/// Handles Relay allocation creation, join code retrieval, joining allocations, and transport configuration.
/// </summary>
public class RelayService
{
    private const string RelayProtocol = "dtls"; // Use dtls for secure, encrypted connections

    /// <summary>
    /// Creates a Relay allocation for hosting.
    /// </summary>
    /// <param name="maxConnections">Maximum number of connections allowed</param>
    /// <returns>The created allocation</returns>
    /// <exception cref="Exception">Thrown if allocation creation fails</exception>
    public async Task<Allocation> CreateAllocationAsync(int maxConnections)
    {
        if (maxConnections < 1)
        {
            throw new ArgumentException("maxConnections must be at least 1", nameof(maxConnections));
        }

        try
        {
            Allocation allocation = await Relay.Instance.CreateAllocationAsync(maxConnections);
            Debug.Log($"[RelayService] Allocation created. Region: {allocation.Region}, MaxConnections: {maxConnections}");
            return allocation;
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayService] Failed to create allocation: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// Gets a join code for an allocation.
    /// </summary>
    /// <param name="allocationId">The allocation ID (Guid) to get a join code for</param>
    /// <returns>The join code string</returns>
    /// <exception cref="ArgumentException">Thrown if allocationId is empty</exception>
    /// <exception cref="Exception">Thrown if join code retrieval fails</exception>
    public async Task<string> GetJoinCodeAsync(System.Guid allocationId)
    {
        if (allocationId == System.Guid.Empty)
        {
            throw new ArgumentException("Allocation ID cannot be empty", nameof(allocationId));
        }

        try
        {
            string joinCode = await Relay.Instance.GetJoinCodeAsync(allocationId);
            Debug.Log($"[RelayService] Join code retrieved: {joinCode}");
            return joinCode;
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayService] Failed to get join code: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// Joins an allocation using a join code.
    /// </summary>
    /// <param name="joinCode">The join code to use</param>
    /// <returns>The join allocation</returns>
    /// <exception cref="ArgumentException">Thrown if join code is null or empty</exception>
    /// <exception cref="Exception">Thrown if join fails</exception>
    public async Task<JoinAllocation> JoinAllocationAsync(string joinCode)
    {
        if (string.IsNullOrWhiteSpace(joinCode))
        {
            throw new ArgumentException("Join code cannot be null or empty", nameof(joinCode));
        }

        try
        {
            JoinAllocation allocation = await Relay.Instance.JoinAllocationAsync(joinCode);
            Debug.Log($"[RelayService] Joined allocation. Region: {allocation.Region}, JoinCode: {joinCode}");
            return allocation;
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayService] Failed to join allocation: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// Configures the UnityTransport with Relay server data for hosting.
    /// </summary>
    /// <param name="allocation">The allocation to configure transport with</param>
    /// <exception cref="ArgumentNullException">Thrown if allocation is null</exception>
    /// <exception cref="InvalidOperationException">Thrown if NetworkManager or UnityTransport is missing</exception>
    public void ConfigureTransportForHost(Allocation allocation)
    {
        if (allocation == null)
        {
            throw new ArgumentNullException(nameof(allocation));
        }

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

        try
        {
            RelayServerData relayServerData = new RelayServerData(allocation, RelayProtocol);
            transport.SetRelayServerData(relayServerData);
            Debug.Log($"[RelayService] Transport configured for host with protocol: {RelayProtocol}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayService] Failed to configure transport for host: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// Configures the UnityTransport with Relay server data for joining.
    /// </summary>
    /// <param name="allocation">The join allocation to configure transport with</param>
    /// <exception cref="ArgumentNullException">Thrown if allocation is null</exception>
    /// <exception cref="InvalidOperationException">Thrown if NetworkManager or UnityTransport is missing</exception>
    public void ConfigureTransportForClient(JoinAllocation allocation)
    {
        if (allocation == null)
        {
            throw new ArgumentNullException(nameof(allocation));
        }

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

        try
        {
            RelayServerData relayServerData = new RelayServerData(allocation, RelayProtocol);
            transport.SetRelayServerData(relayServerData);
            Debug.Log($"[RelayService] Transport configured for client with protocol: {RelayProtocol}");
        }
        catch (Exception e)
        {
            Debug.LogError($"[RelayService] Failed to configure transport for client: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// Creates an allocation and gets the join code in one call (convenience method for hosts).
    /// </summary>
    /// <param name="maxConnections">Maximum number of connections allowed</param>
    /// <returns>A tuple containing the allocation and join code</returns>
    public async Task<(Allocation allocation, string joinCode)> CreateAllocationWithJoinCodeAsync(int maxConnections)
    {
        Allocation allocation = await CreateAllocationAsync(maxConnections);
        string joinCode = await GetJoinCodeAsync(allocation.AllocationId);
        return (allocation, joinCode);
    }
}

