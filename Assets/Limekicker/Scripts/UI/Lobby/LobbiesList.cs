using System;
using Unity.Services.Lobbies.Models;
using UnityEngine;

// Lobby browse is deferred post-MVP; menu UI keeps this component disabled.
/// <summary>
/// Refreshes and displays available multiplayer lobbies for browsing.
/// </summary>
public class LobbiesList : MonoBehaviour
{
    [SerializeField] private Transform lobbyItemParent;
    [SerializeField] private LobbyItem lobbyItemPrefab;

    private bool isRefreshing = false;

    private void OnEnable()
    {
        RefreshLobbiesList();
    }

    public async void RefreshLobbiesList()
    {
        if (isRefreshing || !gameObject.scene.isLoaded)
            return;

        isRefreshing = true;

        try
        {
            QueryResponse lobbies = await NetworkSession.QueryAvailableLobbiesAsync(count: 25);

            foreach (Transform child in lobbyItemParent)
            {
                Destroy(child.gameObject);
            }

            foreach (Lobby lobby in lobbies.Results)
            {
                LobbyItem lobbyItem = Instantiate(lobbyItemPrefab, lobbyItemParent);
                lobbyItem.Initialize(this, lobby);
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"Failed to refresh lobbies list: {e.Message}");
        }

        isRefreshing = false;
    }

    public async void JoinASync(Lobby lobby)
    {
        await NetworkSession.JoinLobbyByIdAsync(lobby.Id);
    }
}
