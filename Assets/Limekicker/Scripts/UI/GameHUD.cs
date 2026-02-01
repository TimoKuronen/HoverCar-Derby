using System;
using System.Collections;
using TMPro;
using UnityEngine;
using VContainer;

public enum InfoTextType
{
    None,
    GameState
}

public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }
    public TextMeshProUGUI startCounterText;
    public TextMeshProUGUI GoText;
    public TextMeshProUGUI infoText;
    public InfoTextType debugInfoToDisplay;
    public GameObject pauseMenu;

    private IGameManager gameManager;

    [Inject]
    public void Construct(IGameManager gameManager)
    {
        Debug.Log("GameHUD Constructed");
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

    public IEnumerator AnimateGoText()
    {
        GoText.gameObject.SetActive(true);
        float timer = 0f;
        while (true)
        {
            if (timer > 1f)
                break;

            // pump scale up and down
            GoText.transform.localScale = Vector3.one * (1f + 0.5f * Mathf.Sin(timer * 5f));

            timer += Time.deltaTime;
            yield return null;
        }

        GoText.gameObject.SetActive(false);
        GoText.transform.localScale = Vector3.one;
    }
}
