using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VContainer;

public class GameHUDView : MonoBehaviour, IGameHUDView
{
    [Header("Display")]
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private InfoTextType debugInfoToDisplay;

    [Header("Menus")]
    [SerializeField] private GameObject pauseMenu;
    [SerializeField] private GameObject winMenu;

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
        presenter = new GameHUDPresenter(this, gameManager, debugInfoToDisplay);
        presenter.Initialize();
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

    public void SetDebugText(string text)
    {
        if (infoText != null)
            infoText.text = text;
    }

    public void ShowPauseMenu(bool show)
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(show);
    }

    public void ShowWinMenu(bool show)
    {
        if (winMenu != null)
            winMenu.SetActive(show);
    }

    private void OnDestroy()
    {
        presenter?.Dispose();
    }
}
