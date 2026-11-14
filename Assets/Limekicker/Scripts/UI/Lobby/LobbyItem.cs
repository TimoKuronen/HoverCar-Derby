using TMPro;
using Unity.Services.Multiplayer;
using UnityEngine;

/// <summary>
/// Single lobby entry UI with name, player count, and join action.
/// </summary>
public class LobbyItem : MonoBehaviour
{
    [SerializeField] private TMP_Text lobbyNameText;
    [SerializeField] private TMP_Text playerCountText;

    private LobbiesList lobbiesList;
    private ISessionInfo session;

    public void Initialize(LobbiesList lobbiesList, ISessionInfo session)
    {
        this.lobbiesList = lobbiesList;
        this.session = session;

        lobbyNameText.text = session.Name;
        playerCountText.text = $"{session.MaxPlayers - session.AvailableSlots}/{session.MaxPlayers}";
    }

    public void Join()
    {
        lobbiesList.JoinASync(session);
    }
}
