using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class RoundResultsView : MonoBehaviour, IRoundResultsView
{
    [SerializeField] private GameObject resultsPanelRoot;
    [SerializeField] private GameObject gameplayScorePanel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Transform scoreContainer;
    [SerializeField] private LeaderboardEntity leaderboardEntityPrefab;
    [SerializeField] private Button rematchButton;
    [SerializeField] private Button mainMenuButton;

    private readonly List<LeaderboardEntity> scoreRows = new();
    private RoundResultsPresenter presenter;
    private IScoreManager scoreManager;
    private IGameManager gameManager;

    public event Action OnRematchClicked;
    public event Action OnMainMenuClicked;

    [Inject]
    public void Construct(IScoreManager scoreManager, IGameManager gameManager)
    {
        this.scoreManager = scoreManager;
        this.gameManager = gameManager;
    }

    private void Awake()
    {
        if (resultsPanelRoot == null)
            resultsPanelRoot = gameObject;
    }

    private void Start()
    {
        WireButtons();
        TryResolveDependencies();

        if (scoreManager == null || gameManager == null)
        {
            Debug.LogError("[RoundResultsView] Dependencies were not resolved. Check GameLifetimeScope injection.");
            return;
        }

        presenter = new RoundResultsPresenter(this, scoreManager, gameManager);
        presenter.Initialize();
    }

    private void TryResolveDependencies()
    {
        GameLifetimeScope scope = FindFirstObjectByType<GameLifetimeScope>();
        if (scope == null)
            return;

        if (scoreManager == null || gameManager == null)
            scope.Container.Inject(this);
    }

    private void WireButtons()
    {
        if (rematchButton != null)
            rematchButton.onClick.AddListener(() => OnRematchClicked?.Invoke());
        if (mainMenuButton != null)
            mainMenuButton.onClick.AddListener(() => OnMainMenuClicked?.Invoke());
    }

    public void ShowResults(string title)
    {
        if (titleText != null)
            titleText.text = title;

        if (gameplayScorePanel != null)
            gameplayScorePanel.SetActive(false);

        if (resultsPanelRoot != null)
            resultsPanelRoot.SetActive(true);
    }

    public void HideResults()
    {
        if (resultsPanelRoot != null)
            resultsPanelRoot.SetActive(false);

        if (gameplayScorePanel != null)
            gameplayScorePanel.SetActive(true);

        ClearScoreRows();
    }

    public void ClearScoreRows()
    {
        foreach (LeaderboardEntity row in scoreRows)
        {
            if (row != null)
                Destroy(row.gameObject);
        }

        scoreRows.Clear();
    }

    public void AddScoreRow(ulong clientId, string playerName, int points)
    {
        if (leaderboardEntityPrefab == null || scoreContainer == null)
            return;

        LeaderboardEntity row = Instantiate(leaderboardEntityPrefab, scoreContainer);
        row.Initialise(clientId, playerName, points);
        scoreRows.Add(row);
    }

    private void OnDestroy()
    {
        if (rematchButton != null)
            rematchButton.onClick.RemoveAllListeners();
        if (mainMenuButton != null)
            mainMenuButton.onClick.RemoveAllListeners();

        presenter?.Dispose();
    }
}
