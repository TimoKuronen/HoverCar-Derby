using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Session browser panel. Opening the panel requests a refresh.
/// </summary>
public class LobbiesView : MonoBehaviour, ILobbiesView
{
    [SerializeField] private GameObject panelRoot;
    [SerializeField] private Button openButton;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button refreshButton;
    [SerializeField] private Transform itemParent;
    [SerializeField] private LobbyItem itemPrefab;
    [SerializeField] private GameObject emptyLabel;

    public event Action OnRefreshRequested;
    public event Action<string> OnJoinRequested;

    private readonly List<LobbyItem> items = new List<LobbyItem>();
    private bool isBusy;

    private void Awake()
    {
        if (openButton != null)
            openButton.onClick.AddListener(Open);
        if (closeButton != null)
            closeButton.onClick.AddListener(Close);
        if (refreshButton != null)
            refreshButton.onClick.AddListener(RequestRefresh);
    }

    private void OnDestroy()
    {
        if (openButton != null)
            openButton.onClick.RemoveListener(Open);
        if (closeButton != null)
            closeButton.onClick.RemoveListener(Close);
        if (refreshButton != null)
            refreshButton.onClick.RemoveListener(RequestRefresh);
    }

    private void Open()
    {
        if (panelRoot != null)
            panelRoot.SetActive(true);

        RequestRefresh();
    }

    private void Close()
    {
        if (panelRoot != null)
            panelRoot.SetActive(false);
    }

    public void ShowSessions(IReadOnlyList<SessionListEntry> sessions)
    {
        foreach (LobbyItem item in items)
        {
            Destroy(item.gameObject);
        }

        items.Clear();

        foreach (SessionListEntry session in sessions)
        {
            LobbyItem item = Instantiate(itemPrefab, itemParent);
            item.Initialize(session, RequestJoin);
            item.SetInteractable(!isBusy);
            items.Add(item);
        }

        if (emptyLabel != null)
            emptyLabel.SetActive(items.Count == 0);
    }

    public void SetBusy(bool busy)
    {
        isBusy = busy;

        if (refreshButton != null)
            refreshButton.interactable = !busy;

        foreach (LobbyItem item in items)
        {
            item.SetInteractable(!busy);
        }
    }

    private void RequestRefresh()
    {
        OnRefreshRequested?.Invoke();
    }

    private void RequestJoin(string sessionId)
    {
        OnJoinRequested?.Invoke(sessionId);
    }
}
