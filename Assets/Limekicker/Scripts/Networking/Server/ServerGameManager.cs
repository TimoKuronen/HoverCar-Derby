using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using Unity.Services.Matchmaker.Models;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;
using UnityEngine.SceneManagement;

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

    public async Task StartServerAsync()
    {
        await multiplayAllocationService.BeginServerCheck();

        try
        {
            MatchmakingResults matchmakerPayload = await GetMatchmakerPayload();

            if (matchmakerPayload != null)
            {
                await StartBackfill(matchmakerPayload);
                NetworkServer.OnUserJoined += UserJoined;
                NetworkServer.OnUserLeft += UserLeft;
            }
            else
            {
                Debug.LogWarning("Matchmaker payload timed out");
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"Failed to start server: {e.Message}");
            return;
        }

        if (!NetworkServer.OpenConnection(serverIp, serverPort))
        {
            Debug.LogError("Failed to start server.");
            return;
        }
    }

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

    private void UserJoined(UserData userData)
    {
        backfiller.AddPlayerToMatch(userData);
        multiplayAllocationService.AddPlayer();

        if (!backfiller.NeedsPlayers() && backfiller.IsBackfilling)
        {
            _ = backfiller.StopBackfill();
        }
    }

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
