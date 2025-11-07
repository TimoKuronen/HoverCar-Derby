using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.Matchmaker;
using Unity.Services.Matchmaker.Models;
using UnityEngine;

/// <summary>
/// Result of matchmaking attempt.
/// </summary>
public enum MatchmakerPollingResult
{
    Success,
    TicketCreationError,
    TicketCancellationError,
    TicketRetrievalError,
    MatchAssignmentError
}

/// <summary>
/// Contains matchmaking result with server IP/port if successful.
/// </summary>
public class MatchmakingResult
{
    public string ip;
    public int port;
    public MatchmakerPollingResult result;
    public string resultMessage;
}

/// <summary>
/// CLIENT-SIDE MATCHMAKING SERVICE
/// 
/// Handles matchmaking flow for clients:
/// 1. Client calls Matchmake() with UserData (includes queue preference: solo-queue or team-queue)
/// 2. Creates matchmaking ticket via Unity Matchmaker Service
/// 3. Polls ticket status until match is found or timeout/error
/// 4. When match found, returns server IP/port from MultiplayAssignment
/// 5. Client connects to returned server IP/port
/// 
/// MATCHMAKING PIPELINE:
/// - Client creates ticket -> Matchmaker Service assigns to available dedicated server
/// - Dedicated server (built with UNITY_SERVER) receives allocation via MultiplayAllocationService
/// - Server gets matchmaker payload, starts backfilling via MatchplayBackfiller
/// - Client receives assignment, connects to server
/// 
/// REQUIRES:
/// - Unity Cloud Dashboard: Matchmaker configured with queues (solo-queue, team-queue)
/// - Dedicated server build (Linux) deployed to Multiplay
/// - Server build must have UNITY_SERVER scripting define symbol
/// </summary>
public class MatchplayMatchmaker : IDisposable
{
    private string lastUsedTicket;
    private CancellationTokenSource cancelToken;

    private const int TicketCooldown = 1000;

    public bool IsMatchmaking { get; private set; }

    /// <summary>
    /// Starts matchmaking process. Creates ticket, polls until match found.
    /// Returns server IP/port when match is assigned by matchmaker.
    /// </summary>
    public async Task<MatchmakingResult> Matchmake(UserData data)
    {
        cancelToken = new CancellationTokenSource();

        Debug.Log("Starting Matchmaking with data " + data);
        Debug.Log($"User Auth ID: {data.userAuthId}");
        Debug.Log($"User Game Preferences: {data.userGamePreferences}");
        string queueName = data.userGamePreferences.ToMultiplayQueue();
        CreateTicketOptions createTicketOptions = new CreateTicketOptions(queueName);
        Debug.Log(createTicketOptions.QueueName);

        List<Player> players = new List<Player>
        {
            new Player(data.userAuthId, data.userGamePreferences)
        };

        try
        {
            IsMatchmaking = true;
            CreateTicketResponse createResult = await MatchmakerService.Instance.CreateTicketAsync(players, createTicketOptions);

            lastUsedTicket = createResult.Id;

            try
            {
                while (!cancelToken.IsCancellationRequested)
                {
                    TicketStatusResponse checkTicket = await MatchmakerService.Instance.GetTicketAsync(lastUsedTicket);

                    if (checkTicket.Type == typeof(MultiplayAssignment))
                    {
                        MultiplayAssignment matchAssignment = (MultiplayAssignment)checkTicket.Value;

                        if (matchAssignment.Status == MultiplayAssignment.StatusOptions.Found)
                        {
                            return ReturnMatchResult(MatchmakerPollingResult.Success, "", matchAssignment);
                        }
                        if (matchAssignment.Status == MultiplayAssignment.StatusOptions.Timeout ||
                            matchAssignment.Status == MultiplayAssignment.StatusOptions.Failed)
                        {
                            return ReturnMatchResult(MatchmakerPollingResult.MatchAssignmentError,
                                $"Ticket: {lastUsedTicket} - {matchAssignment.Status} - {matchAssignment.Message}", null);
                        }
                        Debug.Log($"Polled Ticket: {lastUsedTicket} Status: {matchAssignment.Status} ");
                    }

                    await Task.Delay(TicketCooldown);
                }
            }
            catch (MatchmakerServiceException e)
            {
                return ReturnMatchResult(MatchmakerPollingResult.TicketRetrievalError, e.ToString(), null);
            }
        }
        catch (MatchmakerServiceException e)
        {
            return ReturnMatchResult(MatchmakerPollingResult.TicketCreationError, e.ToString(), null);
        }

        return ReturnMatchResult(MatchmakerPollingResult.TicketRetrievalError, "Cancelled Matchmaking", null);
    }

    /// <summary>Cancels active matchmaking ticket and stops polling.</summary>
    public async Task CancelMatchmaking()
    {
        if (!IsMatchmaking) { return; }

        IsMatchmaking = false;

        if (cancelToken.Token.CanBeCanceled)
        {
            cancelToken.Cancel();
        }

        if (string.IsNullOrEmpty(lastUsedTicket)) { return; }

        Debug.Log($"Cancelling {lastUsedTicket}");

        await MatchmakerService.Instance.DeleteTicketAsync(lastUsedTicket);
    }

    /// <summary>Formats matchmaking result with server IP/port or error message.</summary>
    private MatchmakingResult ReturnMatchResult(MatchmakerPollingResult resultErrorType, string message, MultiplayAssignment assignment)
    {
        IsMatchmaking = false;

        if (assignment != null)
        {
            string parsedIp = assignment.Ip;
            int? parsedPort = assignment.Port;
            if (parsedPort == null)
            {
                return new MatchmakingResult
                {
                    result = MatchmakerPollingResult.MatchAssignmentError,
                    resultMessage = $"Port missing? - {assignment.Port}\n-{assignment.Message}"
                };
            }

            return new MatchmakingResult
            {
                result = MatchmakerPollingResult.Success,
                ip = parsedIp,
                port = (int)parsedPort,
                resultMessage = assignment.Message
            };
        }

        return new MatchmakingResult
        {
            result = resultErrorType,
            resultMessage = message
        };
    }

    public void Dispose()
    {
        _ = CancelMatchmaking();

        cancelToken?.Dispose();
    }
}