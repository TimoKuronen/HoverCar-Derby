using TMPro;
using Unity.Services.Lobbies.Models;
using UnityEngine;

/// <summary>
/// Single lobby entry UI with name, player count, and join action.
/// </summary>
public class LobbyItem : MonoBehaviour
{
    [SerializeField] private TMP_Text lobbyNameText;
    [SerializeField] private TMP_Text playerCountText;

    private LobbiesList lobbiesList;
    private Lobby lobby;

    public void Initialize(LobbiesList lobbiesList, Lobby lobby)
    {
        this.lobbiesList = lobbiesList;
        this.lobby = lobby;

        lobbyNameText.text = lobby.Name;
        playerCountText.text = $"{lobby.Players.Count}/{lobby.MaxPlayers}";
    }

    public void Join()
    {
        lobbiesList.JoinASync(lobby);
    }
}
