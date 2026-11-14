using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main menu UI with host, join, and join-code entry controls.
/// </summary>
public class MainMenuView : MonoBehaviour, IMainMenuView
{
    [Header("Session actions")]
    [SerializeField] private Button hostButton;
    [SerializeField] private Button joinButton;
    [SerializeField] private TMP_InputField joinCodeField;

    [Header("Dev toggles")]
    [SerializeField] private Toggle spawnBotToggle;
    [SerializeField] private Toggle skipCountdownToggle;

    // Lobby browse is hidden until the session-based rewrite lands.
    [Header("Deferred UI (hidden for MVP)")]
    [SerializeField] private GameObject lobbiesButtonRoot;
    [SerializeField] private GameObject lobbiesPanelRoot;
    [SerializeField] private GameObject refreshButtonRoot;
    [SerializeField] private LobbiesList lobbiesList;

    public event Action OnHostClicked;
    public event Action OnJoinClicked;

    private bool isBusy;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private const float HandoffPollIntervalSeconds = 0.5f;
    private float nextHandoffPollTime;
    private string lastHandedOffCode;
#endif

    private void Awake()
    {
        HideDeferredUi();
        WireButtons();
    }

    private void Start()
    {
        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        if (!NetworkSession.IsClientInitialized)
        {
            SetBusy(true);
            return;
        }

        WireDevToggles();
    }

    private void Update()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        PollJoinCodeHandoff();
#endif
    }

    public string GetJoinCode()
    {
        return joinCodeField != null ? joinCodeField.text : string.Empty;
    }

    public void SetBusy(bool busy)
    {
        isBusy = busy;

        if (hostButton != null)
            hostButton.interactable = !busy;
        if (joinButton != null)
            joinButton.interactable = !busy;
    }

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private void PollJoinCodeHandoff()
    {
        if (isBusy || Time.unscaledTime < nextHandoffPollTime)
            return;

        nextHandoffPollTime = Time.unscaledTime + HandoffPollIntervalSeconds;

        if (!JoinCodeHandoff.TryRead(out string joinCode) || joinCode == lastHandedOffCode)
            return;

        lastHandedOffCode = joinCode;

        if (joinCodeField != null)
            joinCodeField.text = joinCode;

        if (JoinCodeHandoff.AutoJoinEnabled)
            OnJoinClicked?.Invoke();
    }
#endif

    private void HideDeferredUi()
    {
        SetActiveIfAssigned(lobbiesButtonRoot, false);
        SetActiveIfAssigned(lobbiesPanelRoot, false);
        SetActiveIfAssigned(refreshButtonRoot, false);

        if (lobbiesList != null)
            lobbiesList.enabled = false;
    }

    private void WireButtons()
    {
        if (hostButton != null)
            hostButton.onClick.AddListener(() => OnHostClicked?.Invoke());
        if (joinButton != null)
            joinButton.onClick.AddListener(() => OnJoinClicked?.Invoke());
    }

    private void WireDevToggles()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (spawnBotToggle != null)
        {
            spawnBotToggle.isOn = DevMenuOptions.IsSpawnBotEnabled();
            spawnBotToggle.onValueChanged.AddListener(DevMenuOptions.SetSpawnBotEnabled);
        }

        if (skipCountdownToggle != null)
        {
            skipCountdownToggle.isOn = DevMenuOptions.IsSkipCountdownEnabled();
            skipCountdownToggle.onValueChanged.AddListener(DevMenuOptions.SetSkipCountdownEnabled);
        }
#else
        if (spawnBotToggle != null)
            spawnBotToggle.gameObject.SetActive(false);
        if (skipCountdownToggle != null)
            skipCountdownToggle.gameObject.SetActive(false);
#endif
    }

    private static void SetActiveIfAssigned(GameObject target, bool active)
    {
        if (target != null)
            target.SetActive(active);
    }

    private void OnDestroy()
    {
        if (hostButton != null)
            hostButton.onClick.RemoveAllListeners();
        if (joinButton != null)
            joinButton.onClick.RemoveAllListeners();

#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (spawnBotToggle != null)
            spawnBotToggle.onValueChanged.RemoveAllListeners();
        if (skipCountdownToggle != null)
            skipCountdownToggle.onValueChanged.RemoveAllListeners();
#endif
    }
}
