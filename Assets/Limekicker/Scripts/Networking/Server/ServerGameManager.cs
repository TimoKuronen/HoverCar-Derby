using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

/// <summary>
/// DEDICATED SERVER: Game Manager
/// 
/// Manages dedicated server lifecycle for matchmaking:
/// 1. Waits for Multiplay allocation (server instance assigned by Multiplay)
/// 2. Receives matchmaker payload (queue name, initial players, backfill ticket ID)
/// 3. Starts backfilling to fill remaining slots
/// 4. Opens network connection for clients to join
/// 5. Manages player join/leave events, updates backfill ticket
/// 
/// SERVER STARTUP FLOW:
/// - ApplicationController (NetBootstrap scene) detects headless build
/// - Creates ServerSingleton, loads PlayScene
/// - ServerSingleton.CreateServer() initializes this manager
/// - StartServerAsync() begins allocation wait and matchmaker integration
/// 
/// MATCHMAKING INTEGRATION:
/// - MultiplayAllocationService: Waits for allocation, gets matchmaker payload
/// - MatchplayBackfiller: Manages backfilling to fill match slots
/// - NetworkServer: Handles client connections and player spawning
/// 
/// BUILD REQUIREMENTS:
/// - Linux headless build
/// - UNITY_SERVER scripting define symbol
/// - Deployed to Multiplay fleet
/// </summary>
public class ServerGameManager : IDisposable
{
    private string serverIp;
    private int serverPort;
    private int serverQPort;
    private MatchplayBackfiller backfiller;
    private MultiplayAllocationService multiplayAllocationService;
    public NetworkServer NetworkServer { get; private set; }

    public ServerGameManager(string serverIp, int serverPort, int serverQPort, NetworkManager manager, NetworkObject playerPrefab)
    {
        this.serverIp = serverIp;
        this.serverPort = serverPort;
        this.serverQPort = serverQPort;
        NetworkServer = new NetworkServer(manager, playerPrefab);
        multiplayAllocationService = new MultiplayAllocationService();
    }

    /// <summary>
    /// Starts dedicated server with matchmaking integration.
    /// 1. Begins server health check (heartbeat to Multiplay)
    /// 2. Waits for matchmaker allocation (up to 20s timeout)
    /// 3. Starts backfilling with matchmaker payload
    /// 4. Opens network connection for clients
    /// </summary>
    public async Task StartServerAsync()
    {
        // Start server health check (reports server status to Multiplay)
        await multiplayAllocationService.BeginServerCheck();
#if UNITY_SERVER
        // Advertise basic server data to query handler (optional but helpful)
        multiplayAllocationService.SetMaxPlayers(20);
        multiplayAllocationService.SetServerName("HoverCar Server");
#endif

        try
        {
            // Wait for matchmaker to assign this server to a match
            // Returns payload with queue name, initial players, backfill ticket ID
            MatchmakingResults matchmakerPayload = await GetMatchmakerPayload();

            if (matchmakerPayload != null)
            {
                // Start backfilling to fill remaining match slots
                await StartBackfill(matchmakerPayload);

                // Subscribe to player events for backfill management
                NetworkServer.OnUserJoined += UserJoined;
                NetworkServer.OnUserLeft += UserLeft;
            }
            else
            {
                Debug.LogWarning("Matchmaker payload timed out - server may not receive match assignment");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to start server: {e.Message}");
            return;
        }

        // Open network connection (clients will connect via IP/port from matchmaker)
        if (!NetworkServer.OpenConnection(serverIp, serverPort))
        {
            Debug.LogError("Failed to start server.");
            return;
        }
    }

    /// <summary>
    /// Waits for matchmaker allocation payload (20s timeout).
    /// Server must be allocated by Multiplay and receive matchmaker assignment.
    /// </summary>
    private async Task<MatchmakingResults> GetMatchmakerPayload()
    {
        Task<MatchmakingResults> matchmakerPayloadTask = multiplayAllocationService.SubscribeAndAwaitMatchmakerAllocation();
        if (await Task.WhenAny(matchmakerPayloadTask, Task.Delay(20000)) == matchmakerPayloadTask)
        {
            // Completed within timeout
            return matchmakerPayloadTask.Result;
        }

        Debug.LogError("Matchmaker allocation timed out.");
        return null;
    }
    /// <summary>Initializes backfiller and starts backfilling if match needs more players.</summary>
    private async Task StartBackfill(MatchmakingResults matchmakerPayload)
    {
        backfiller = new MatchplayBackfiller($"{serverIp}:{serverQPort}",
            matchmakerPayload.QueueName,
            matchmakerPayload.MatchProperties,
            20);

        if (backfiller.NeedsPlayers())
        {
            await backfiller.BeginBackfilling();
        }
    }

    /// <summary>Adds player to backfill ticket and updates server player count. Stops backfilling if match is full.</summary>
    private void UserJoined(UserData userData)
    {
        backfiller.AddPlayerToMatch(userData);
        multiplayAllocationService.AddPlayer();

        if (!backfiller.NeedsPlayers() && backfiller.IsBackfilling)
        {
            _ = backfiller.StopBackfill();
        }
    }

    /// <summary>Removes player from backfill ticket. Closes server if empty, resumes backfilling if needed.</summary>
    private void UserLeft(UserData userData)
    {
        int playerCount = backfiller.RemovePlayerFromMatch(userData.userAuthId);
        multiplayAllocationService.RemovePlayer();

        if (playerCount <= 0)
        {
            CloseServer();
            return;
        }

        if (backfiller.NeedsPlayers() && !backfiller.IsBackfilling)
        {
            _ = backfiller.BeginBackfilling();
        }
    }

    /// <summary>Stops backfilling and shuts down server when match becomes empty.</summary>
    private async void CloseServer()
    {
        await backfiller.StopBackfill();
        Dispose();
        Application.Quit();
    }

    public void Dispose()
    {
        NetworkServer.OnUserJoined -= UserJoined;
        NetworkServer.OnUserLeft -= UserLeft;

        backfiller?.Dispose();
        multiplayAllocationService?.Dispose();
        NetworkServer?.Dispose();
    }
}
