using System;
using UnityEngine;
using UnityEngine.UI;

public class PauseMenuView : MonoBehaviour, IPauseMenuView
{
    [Header("Menus")]
    [SerializeField] private GameObject pauseMenu;

    [Header("Pause menu buttons")]
    [SerializeField] private Button pauseMenuButton;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button goToMenuFromPauseButton;

    public event Action OnPauseMenuClicked;
    public event Action OnResumeClicked;
    public event Action OnGoToMenuClicked;

    private void Awake()
    {
        WireButtons();
    }

    private void WireButtons()
    {
        if (pauseMenuButton != null)
            pauseMenuButton.onClick.AddListener(() => OnPauseMenuClicked?.Invoke());
        if (resumeButton != null)
            resumeButton.onClick.AddListener(() => OnResumeClicked?.Invoke());
        if (goToMenuFromPauseButton != null)
            goToMenuFromPauseButton.onClick.AddListener(() => OnGoToMenuClicked?.Invoke());
    }

    public void ShowPauseMenu(bool show)
    {
        if (pauseMenu != null)
            pauseMenu.SetActive(show);
    }

    private void OnDestroy()
    {
        if (pauseMenuButton != null)
            pauseMenuButton.onClick.RemoveAllListeners();
        if (resumeButton != null)
            resumeButton.onClick.RemoveAllListeners();
        if (goToMenuFromPauseButton != null)
            goToMenuFromPauseButton.onClick.RemoveAllListeners();
    }
}
