using System;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class GameHUDView : MonoBehaviour, IGameHUDView
{
    [Header("Menus")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject resultsMenu;

    [Header("Pause menu buttons")]
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button goToMenuFromPauseButton;

    [Header("Win menu buttons")]
    [SerializeField] private Button restartButton;
    [SerializeField] private Button goToMenuFromWinButton;

    private IGameManager gameManager;
    private GameHUDPresenter presenter;

    public event Action OnResumeClicked;
    public event Action OnRestartClicked;
    public event Action OnGoToMenuClicked;

    [Inject]
    public void Construct(IGameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    private void Start()
    {
        WireButtons();
        TryResolveDependencies();

        if (gameManager == null)
        {
            Debug.LogError("[GameHUDView] IGameManager was not resolved. Check GameLifetimeScope injection.");
            return;
        }

        presenter = new GameHUDPresenter(this, gameManager);
        presenter.Initialize();
    }

    private void TryResolveDependencies()
    {
        if (gameManager != null)
            return;

        GameLifetimeScope scope = FindFirstObjectByType<GameLifetimeScope>();
        scope?.Container.Inject(this);
    }

    private void WireButtons()
    {
        if (resumeButton != null)
            resumeButton.onClick.AddListener(() => OnResumeClicked?.Invoke());
        if (goToMenuFromPauseButton != null)
            goToMenuFromPauseButton.onClick.AddListener(() => OnGoToMenuClicked?.Invoke());
        if (restartButton != null)
            restartButton.onClick.AddListener(() => OnRestartClicked?.Invoke());
        if (goToMenuFromWinButton != null)
            goToMenuFromWinButton.onClick.AddListener(() => OnGoToMenuClicked?.Invoke());
    }

    public void ShowPauseMenu(bool show)
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(show);
    }

    public void ShowResultsMenu(bool show)
    {
        if (resultsMenu != null)
            resultsMenu.SetActive(show);
    }

    private void OnDestroy()
    {
        presenter?.Dispose();
    }
}
