using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

#if UNITY_SERVER
using Newtonsoft.Json;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Multiplay;
#else
using Unity.Services.Matchmaker.Models; // needed for the stub return type
#endif

/// <summary>
/// DEDICATED SERVER: Multiplay Allocation Service
/// 
/// Handles server allocation and matchmaker integration for dedicated servers.
/// 
/// HOW IT WORKS:
/// 1. Server build (Linux) deployed to Multiplay with UNITY_SERVER define
/// 2. Multiplay allocates server instance when matchmaker assigns players
/// 3. Server receives allocation ID and matchmaker payload via SubscribeAndAwaitMatchmakerAllocation()
/// 4. Payload contains: QueueName, MatchProperties (players, teams, backfill ticket ID)
/// 5. Server uses payload to start backfilling (MatchplayBackfiller) and manage match
/// 
/// BUILD REQUIREMENTS:
/// - Build target: Linux (headless)
/// - Scripting define: UNITY_SERVER (set in Player Settings > Other Settings)
/// - Unity Cloud Dashboard: Multiplay fleet configured
/// - Matchmaker queues configured (solo-queue, team-queue)
/// 
/// DEPLOYMENT PIPELINE:
/// 1. Build Linux server (File > Build Settings > Linux > Build)
/// 2. Upload to Multiplay (via Unity Cloud Dashboard or CLI)
/// 3. Configure fleet with server build
/// 4. Matchmaker will allocate servers when clients request matches
/// 
/// NOTE: In client/editor builds, this is a stub (no-op) since UNITY_SERVER is not defined.
/// </summary>
public class MultiplayAllocationService : IDisposable
{
#if UNITY_SERVER
    private IMultiplayService multiplayService;
    private MultiplayEventCallbacks serverCallbacks;
    private IServerQueryHandler serverCheckManager;
    private IServerEvents serverEvents;
    private CancellationTokenSource serverCheckCancel;
    private string allocationId;
#endif

    // Constructor ----------------------------------------------------------------
    public MultiplayAllocationService()
    {
#if UNITY_SERVER
        try
        {
            multiplayService = MultiplayService.Instance;
            serverCheckCancel = new CancellationTokenSource();
            Debug.Log("[Multiplay] Allocation service initialized (server).");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Multiplay] Failed to initialize.\n{ex}");
        }
#else
        Debug.Log("[Multiplay] Allocation service stub initialized (client).");
#endif
    }

    // SERVER IMPLEMENTATION ------------------------------------------------------
#if UNITY_SERVER
    /// <summary>Subscribes to Multiplay events, waits for allocation, and retrieves matchmaker payload.</summary>
    public async Task<MatchmakingResults> SubscribeAndAwaitMatchmakerAllocation()
    {
        if (multiplayService == null)
            return null;

        allocationId = null;
        serverCallbacks = new MultiplayEventCallbacks();
        serverCallbacks.Allocate += OnMultiplayAllocation;
        serverCallbacks.Deallocate += OnMultiplayDeAllocation;
        serverCallbacks.Error += OnMultiplayError;

        serverEvents = await multiplayService.SubscribeToServerEventsAsync(serverCallbacks);

        await AwaitAllocationID();
        var matchmakingPayload = await GetMatchmakerAllocationPayloadAsync();
        return matchmakingPayload;
    }

    /// <summary>Polls ServerConfig until allocation ID is available.</summary>
    private async Task AwaitAllocationID()
    {
        var config = multiplayService.ServerConfig;
        Debug.Log($"[Multiplay] Awaiting Allocation...\n" +
                  $"- ServerID: {config.ServerId}\n" +
                  $"- AllocationID: {config.AllocationId}\n" +
                  $"- Port: {config.Port}\n" +
                  $"- QPort: {config.QueryPort}\n" +
                  $"- LogDir: {config.ServerLogDirectory}");

        while (string.IsNullOrEmpty(allocationId))
        {
            string configID = config.AllocationId;
            if (!string.IsNullOrEmpty(configID))
            {
                Debug.Log($"[Multiplay] Config AllocationID: {configID}");
                allocationId = configID;
            }
            await Task.Delay(100);
        }
    }

    /// <summary>Retrieves matchmaker payload from Multiplay allocation JSON.</summary>
    private async Task<MatchmakingResults> GetMatchmakerAllocationPayloadAsync()
    {
        var payload = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>();
        var json = JsonConvert.SerializeObject(payload, Formatting.Indented);
        Debug.Log("[Multiplay] Matchmaker Allocation Payload:\n" + json);
        return payload;
    }

    /// <summary>Handles Multiplay allocation event.</summary>
    private void OnMultiplayAllocation(MultiplayAllocation allocation)
    {
        Debug.Log($"[Multiplay] OnAllocation: {allocation.AllocationId}");
        if (!string.IsNullOrEmpty(allocation.AllocationId))
            allocationId = allocation.AllocationId;
    }

    /// <summary>Handles Multiplay deallocation event.</summary>
    private void OnMultiplayDeAllocation(MultiplayDeallocation deallocation)
    {
        Debug.Log($"[Multiplay] Deallocated: {deallocation.AllocationId} (Server {deallocation.ServerId})");
    }

    /// <summary>Handles Multiplay error events.</summary>
    private void OnMultiplayError(MultiplayError error)
    {
        Debug.LogError($"[Multiplay] Error: {error.Reason}\n{error.Detail}");
    }

    /// <summary>Starts server health check loop that reports status to Multiplay.</summary>
    public async Task BeginServerCheck()
    {
        if (multiplayService == null)
            return;

        serverCheckManager = await multiplayService.StartServerQueryHandlerAsync((ushort)20, "Server", "", "0", "");
        ServerCheckLoop(serverCheckCancel.Token);
    }

    /// <summary>Continuously updates server status for Multiplay query handler.</summary>
    private async void ServerCheckLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            serverCheckManager.UpdateServerCheck();
            await Task.Delay(100);
        }
    }

    public void SetServerName(string name) => serverCheckManager.ServerName = name;
    public void SetBuildID(string id) => serverCheckManager.BuildId = id;
    public void SetMaxPlayers(ushort players) => serverCheckManager.MaxPlayers = players;
    public void AddPlayer() => serverCheckManager.CurrentPlayers++;
    public void RemovePlayer() => serverCheckManager.CurrentPlayers--;
    public void SetMap(string map) => serverCheckManager.Map = map;
    public void SetMode(string mode) => serverCheckManager.GameType = mode;
#endif

    // CLIENT STUBS ---------------------------------------------------------------
#if !UNITY_SERVER
    public Task<MatchmakingResults> SubscribeAndAwaitMatchmakerAllocation()
    {
        Debug.Log("[Multiplay Stub] SubscribeAndAwaitMatchmakerAllocation() called on client build.");
        return Task.FromResult<MatchmakingResults>(null);
    }

    public Task BeginServerCheck()
    {
        Debug.Log("[Multiplay Stub] BeginServerCheck() called on client build.");
        return Task.CompletedTask;
    }

    public void SetServerName(string name) { }
    public void SetBuildID(string id) { }
    public void SetMaxPlayers(ushort players) { }
    public void AddPlayer() { }
    public void RemovePlayer() { }
    public void SetMap(string map) { }
    public void SetMode(string mode) { }
#endif

    // DISPOSAL -------------------------------------------------------------------
    public void Dispose()
    {
#if UNITY_SERVER
        try
        {
            if (serverCallbacks != null)
            {
                serverCallbacks.Allocate -= OnMultiplayAllocation;
                serverCallbacks.Deallocate -= OnMultiplayDeAllocation;
                serverCallbacks.Error -= OnMultiplayError;
            }
            serverCheckCancel?.Cancel();
            _ = serverEvents?.UnsubscribeAsync();
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Multiplay] Dispose failed: {ex}");
        }
#else
        // No-op for clients
#endif
    }
}
