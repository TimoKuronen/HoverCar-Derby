using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Single session row with name, player count, and a join action.
/// </summary>
public class LobbyItem : MonoBehaviour
{
    [SerializeField] private TMP_Text lobbyNameText;
    [SerializeField] private TMP_Text playerCountText;
    [SerializeField] private Button joinButton;

    private Action<string> onJoin;
    private string sessionId;

    private void Awake()
    {
        if (joinButton == null)
            joinButton = GetComponentInChildren<Button>(true);

        if (joinButton != null)
            joinButton.onClick.AddListener(HandleJoinClicked);
    }

    private void OnDestroy()
    {
        if (joinButton != null)
            joinButton.onClick.RemoveListener(HandleJoinClicked);
    }

    public void Initialize(SessionListEntry entry, Action<string> onJoin)
    {
        sessionId = entry.Id;
        this.onJoin = onJoin;

        lobbyNameText.text = entry.Name;
        playerCountText.text = $"{entry.PlayerCount}/{entry.MaxPlayers}";
    }

    public void SetInteractable(bool interactable)
    {
        if (joinButton != null)
            joinButton.interactable = interactable;
    }

    private void HandleJoinClicked()
    {
        onJoin?.Invoke(sessionId);
    }
}
