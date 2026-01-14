using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

/// <summary>
/// Centralized service for Unity Lobbies operations.
/// Handles lobby creation, joining, querying, heartbeat, and player management.
/// </summary>
public class LobbyService : IDisposable
{
    private string currentLobbyId;
    private Coroutine heartbeatCoroutine;
    private MonoBehaviour coroutineRunner;

    /// <summary>Gets the ID of the currently active lobby, if any.</summary>
    public string CurrentLobbyId => currentLobbyId;

    /// <summary>Gets whether a lobby is currently active.</summary>
    public bool HasActiveLobby => !string.IsNullOrEmpty(currentLobbyId);

    public LobbyService(MonoBehaviour coroutineRunner = null)
    {
        this.coroutineRunner = coroutineRunner;
    }

    /// <summary>
    /// Creates a new lobby with the specified join code and settings.
    /// </summary>
    /// <param name="lobbyName">Name of the lobby</param>
    /// <param name="maxPlayers">Maximum number of players</param>
    /// <param name="joinCode">Relay join code to store in lobby data</param>
    /// <param name="isPrivate">Whether the lobby is private</param>
    /// <returns>The created lobby</returns>
    public async Task<Lobby> CreateLobbyAsync(string lobbyName, int maxPlayers, string joinCode, bool isPrivate = false)
    {
        try
        {
            CreateLobbyOptions lobbyOptions = new CreateLobbyOptions
            {
                IsPrivate = isPrivate,
                Data = new Dictionary<string, DataObject>()
                {
                    {
                        "JoinCode", new DataObject(
                            visibility: DataObject.VisibilityOptions.Member,
                            value: joinCode
                        )
                    }
                }
            };

            Lobby lobby = await Lobbies.Instance.CreateLobbyAsync(lobbyName, maxPlayers, lobbyOptions);
            currentLobbyId = lobby.Id;
            Debug.Log($"[LobbyService] Created lobby: {lobby.Name} (ID: {lobby.Id})");
            return lobby;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyService] Failed to create lobby: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// Joins a lobby by its ID.
    /// </summary>
    /// <param name="lobbyId">The ID of the lobby to join</param>
    /// <returns>The joined lobby</returns>
    public async Task<Lobby> JoinLobbyByIdAsync(string lobbyId)
    {
        try
        {
            Lobby lobby = await Lobbies.Instance.JoinLobbyByIdAsync(lobbyId);
            currentLobbyId = lobby.Id;
            Debug.Log($"[LobbyService] Joined lobby: {lobby.Name} (ID: {lobby.Id})");
            return lobby;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyService] Failed to join lobby: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// Queries available lobbies with the specified filters.
    /// </summary>
    /// <param name="count">Maximum number of lobbies to return</param>
    /// <param name="filters">Optional filters to apply</param>
    /// <returns>Query response with matching lobbies</returns>
    public async Task<QueryResponse> QueryLobbiesAsync(int count = 25, List<QueryFilter> filters = null)
    {
        try
        {
            QueryLobbiesOptions options = new QueryLobbiesOptions
            {
                Count = count
            };

            if (filters != null && filters.Count > 0)
            {
                options.Filters = filters;
            }

            QueryResponse response = await Lobbies.Instance.QueryLobbiesAsync(options);
            Debug.Log($"[LobbyService] Queried lobbies: found {response.Results.Count} results");
            return response;
        }
        catch (LobbyServiceException e)
        {
            Debug.LogError($"[LobbyService] Failed to query lobbies: {e.Message}");
            throw;
        }
    }

    /// <summary>
    /// Queries for available lobbies with default filters (has available slots, not locked).
    /// </summary>
    /// <param name="count">Maximum number of lobbies to return</param>
    /// <returns>Query response with matching lobbies</returns>
    public async Task<QueryResponse> QueryAvailableLobbiesAsync(int count = 25)
    {
        List<QueryFilter> filters = new List<QueryFilter>()
        {
            new QueryFilter
            (
                field: QueryFilter.FieldOptions.AvailableSlots,
                op: QueryFilter.OpOptions.GT,
                value: "0"
            ),
            new QueryFilter
            (
                field: QueryFilter.FieldOptions.IsLocked,
                op: QueryFilter.OpOptions.EQ,
                value: "0"
            )
        };

        return await QueryLobbiesAsync(count, filters);
    }

    /// <summary>
    /// Deletes the current lobby. Handles errors gracefully during shutdown.
    /// </summary>
    public async Task DeleteLobbyAsync()
    {
        if (string.IsNullOrEmpty(currentLobbyId))
            return;

        string lobbyIdToDelete = currentLobbyId;
        currentLobbyId = null;

        StopHeartbeat();

        try
        {
            await Lobbies.Instance.DeleteLobbyAsync(lobbyIdToDelete);
            Debug.Log($"[LobbyService] Deleted lobby: {lobbyIdToDelete}");
        }
        catch (LobbyServiceException e)
        {
            // "lobby not found" is expected during shutdown - don't log as error
            if (e.Message.Contains("not found") || e.Message.Contains("lobby not found"))
            {
                Debug.Log($"[LobbyService] Lobby already deleted (expected during shutdown)");
            }
            else
            {
                Debug.LogWarning($"[LobbyService] Failed to delete lobby: {e.Message}");
            }
        }
        catch (Exception e)
        {
            // Handle any other exceptions gracefully during shutdown
            Debug.LogWarning($"[LobbyService] Exception during lobby deletion (expected during shutdown): {e.Message}");
        }
    }

    /// <summary>
    /// Removes a player from the current lobby by their authentication ID.
    /// </summary>
    /// <param name="authId">The authentication ID of the player to remove</param>
    public async Task RemovePlayerAsync(string authId)
    {
        if (string.IsNullOrEmpty(currentLobbyId))
            return;

        try
        {
            await Lobbies.Instance.RemovePlayerAsync(currentLobbyId, authId);
            Debug.Log($"[LobbyService] Removed player from lobby: {authId}");
        }
        catch (LobbyServiceException e)
        {
            // "lobby not found" is expected during shutdown - don't log as error
            if (e.Message.Contains("not found") || e.Message.Contains("lobby not found"))
            {
                Debug.Log($"[LobbyService] Lobby already deleted when removing player (expected during shutdown)");
            }
            else
            {
                Debug.LogWarning($"[LobbyService] Failed to remove player from lobby: {e.Message}");
            }
        }
        catch (Exception e)
        {
            // Handle any other exceptions gracefully during shutdown
            Debug.LogWarning($"[LobbyService] Exception during player removal (expected during shutdown): {e.Message}");
        }
    }

    /// <summary>
    /// Starts sending heartbeat pings to keep the lobby alive.
    /// </summary>
    /// <param name="intervalSeconds">Interval between heartbeats in seconds</param>
    /// <param name="coroutineRunner">MonoBehaviour to run the coroutine on (uses current if null)</param>
    public void StartHeartbeat(float intervalSeconds, MonoBehaviour coroutineRunner = null)
    {
        if (string.IsNullOrEmpty(currentLobbyId))
        {
            Debug.LogWarning("[LobbyService] Cannot start heartbeat: no active lobby");
            return;
        }

        StopHeartbeat();

        MonoBehaviour runner = coroutineRunner ?? this.coroutineRunner;
        if (runner == null)
        {
            Debug.LogError("[LobbyService] Cannot start heartbeat: no coroutine runner provided");
            return;
        }

        heartbeatCoroutine = runner.StartCoroutine(HeartbeatCoroutine(intervalSeconds));
        Debug.Log($"[LobbyService] Started heartbeat for lobby: {currentLobbyId} (interval: {intervalSeconds}s)");
    }

    /// <summary>
    /// Stops the heartbeat coroutine if it's running.
    /// </summary>
    public void StopHeartbeat()
    {
        if (heartbeatCoroutine != null && coroutineRunner != null)
        {
            coroutineRunner.StopCoroutine(heartbeatCoroutine);
            heartbeatCoroutine = null;
        }
    }

    /// <summary>
    /// Coroutine that sends heartbeat pings at the specified interval.
    /// </summary>
    private IEnumerator HeartbeatCoroutine(float waitTimeSeconds)
    {
        WaitForSecondsRealtime delay = new WaitForSecondsRealtime(waitTimeSeconds);

        while (!string.IsNullOrEmpty(currentLobbyId))
        {
            try
            {
                Lobbies.Instance.SendHeartbeatPingAsync(currentLobbyId);
            }
            catch (Exception e)
            {
                // Lobby might be deleted or service unavailable during shutdown
                Debug.LogWarning($"[LobbyService] Heartbeat failed (expected during shutdown): {e.Message}");
                yield break;
            }

            yield return delay;
        }
    }

    /// <summary>
    /// Clears the current lobby reference without deleting it.
    /// Useful when leaving a lobby without deleting it.
    /// </summary>
    public void ClearCurrentLobby()
    {
        StopHeartbeat();
        currentLobbyId = null;
    }

    public void Dispose()
    {
        StopHeartbeat();
        // Note: We don't delete the lobby here automatically
        // Call DeleteLobbyAsync() explicitly if needed
    }
}


