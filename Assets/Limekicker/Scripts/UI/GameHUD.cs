using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }

    public TextMeshProUGUI infoText;
    public InfoTextType debugInfoToDisplay;
    public GameObject pauseMenu;

    private IGameManager gameManager;

    [Inject]
    public void Construct(IGameManager gameManager)
    {
        this.gameManager = gameManager;
    }

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }

        Instance = this;
    }

    private void Update()
    {
        UpdateInfoText();

        if (Keyboard.current.iKey.wasPressedThisFrame)
        {
            Debug.Log(gameManager.CurrentGameState.ToString());
        }
    }

    private void UpdateInfoText()
    {
        if (debugInfoToDisplay == InfoTextType.None || gameManager.CurrentGameState == null)
            return;

        // Update info text based on selected type
        switch (debugInfoToDisplay)
        {
            case InfoTextType.None:
                infoText.text = "";
                break;
            case InfoTextType.GameState:
                infoText.text = gameManager.CurrentGameState.ToString();
                break;
        }
    }

    public void LeaveGame()
    {
        NetworkSession.LeaveGame();
    }
}

public enum InfoTextType
{
    None,
    GameState
}