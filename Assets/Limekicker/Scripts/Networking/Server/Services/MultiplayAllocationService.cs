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
/// Handles Multiplay allocation and server heartbeat.
/// Fully functional in dedicated server builds.
/// Behaves as a harmless stub in client/mobile builds.
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

    private async Task<MatchmakingResults> GetMatchmakerAllocationPayloadAsync()
    {
        var payload = await MultiplayService.Instance.GetPayloadAllocationFromJsonAs<MatchmakingResults>();
        var json = JsonConvert.SerializeObject(payload, Formatting.Indented);
        Debug.Log("[Multiplay] Matchmaker Allocation Payload:\n" + json);
        return payload;
    }

    private void OnMultiplayAllocation(MultiplayAllocation allocation)
    {
        Debug.Log($"[Multiplay] OnAllocation: {allocation.AllocationId}");
        if (!string.IsNullOrEmpty(allocation.AllocationId))
            allocationId = allocation.AllocationId;
    }

    private void OnMultiplayDeAllocation(MultiplayDeallocation deallocation)
    {
        Debug.Log($"[Multiplay] Deallocated: {deallocation.AllocationId} (Server {deallocation.ServerId})");
    }

    private void OnMultiplayError(MultiplayError error)
    {
        Debug.LogError($"[Multiplay] Error: {error.Reason}\n{error.Detail}");
    }

    public async Task BeginServerCheck()
    {
        if (multiplayService == null)
            return;

        serverCheckManager = await multiplayService.StartServerQueryHandlerAsync((ushort)20, "Server", "", "0", "");
        ServerCheckLoop(serverCheckCancel.Token);
    }

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
