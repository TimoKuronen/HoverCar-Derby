using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Services.Lobbies;
using Unity.Services.Lobbies.Models;
using UnityEngine;

public class LobbiesList : MonoBehaviour
{
    [SerializeField] private MainMenu mainMenu;
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
            QueryLobbiesOptions options = new QueryLobbiesOptions();

            options.Count = 25;

            options.Filters = new List<QueryFilter>()
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

            QueryResponse lobbies = await Lobbies.Instance.QueryLobbiesAsync(options);

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
