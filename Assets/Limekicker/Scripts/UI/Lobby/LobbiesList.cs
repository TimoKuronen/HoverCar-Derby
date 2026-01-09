using System;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbiesList : MonoBehaviour
{
    [SerializeField] private MainMenu mainMenu;
    [SerializeField] private Transform lobbyItemParent;
    [SerializeField] private LobbyItem lobbyItemPrefab;

    private bool isRefreshing = false;
    private LobbyService lobbyService;

    private void Start()
    {
        lobbyService = new LobbyService(this);
    }

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
            QueryResponse lobbies = await lobbyService.QueryAvailableLobbiesAsync(count: 25);

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

    public void JoinASync(Lobby lobby)
    {
        mainMenu.JoinASync(lobby);
    }
}
