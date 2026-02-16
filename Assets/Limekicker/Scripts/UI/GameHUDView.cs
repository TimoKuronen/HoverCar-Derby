using TMPro;
using UnityEngine;
using VContainer;

public class GameHUDView : MonoBehaviour, IGameHUDView
{
    [SerializeField] private TextMeshProUGUI infoText;
    [SerializeField] private InfoTextType debugInfoToDisplay;
    [SerializeField] private GameObject pauseMenu;

    private IGameManager gameManager;
    private GameHUDPresenter presenter;

    [Inject]
    public void Construct(IGameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    private void Start()
    {
        presenter = new GameHUDPresenter(this, gameManager, debugInfoToDisplay);
        presenter.Initialize();
    }

    public void SetDebugText(string text)
    {
        if (infoText != null)
        {
            infoText.text = text;
        }
    }

    public void ShowPauseMenu(bool show)
    {
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(show);
        }
    }

    public void LeaveGame()
    {
        NetworkSession.LeaveGame();
    }

    private void OnDestroy()
    {
        presenter?.Dispose();
    }
}
