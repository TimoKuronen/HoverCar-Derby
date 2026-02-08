using System;
using System.Text;
using TMPro;
using UnityEngine;
using VContainer;

public class MatchDurationDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI matchDurationText;

    private IGameManager gameManager;

    private EventBinding<GameStateChangeEvent> gameStateChangeEvent;

    [Inject]
    public void Construct(IGameManager gameManager)
    {
        this.gameManager = gameManager;
        this.gameManager.OnGameTimerUpdated += UpdateTimeDisplay;
    }

    private void Start()
    {
        UpdateTimeDisplay(gameManager.Context.roundDurationInSeconds);
        matchDurationText.gameObject.SetActive(false);

        gameStateChangeEvent = new EventBinding<GameStateChangeEvent>(HandleGameStateChange);
        EventBus<GameStateChangeEvent>.Register(gameStateChangeEvent);
    }

    private void HandleGameStateChange(GameStateChangeEvent @event)
    {
        if(@event.NewState is PlayState || @event.NewState is CountdownState)
        {
            matchDurationText.gameObject.SetActive(true);
        }
        else
        {
            matchDurationText.gameObject.SetActive(false);
        }
    }

    private void UpdateTimeDisplay(int obj)
    {
        StringBuilder sb = new();
        int minutes = obj / 60;
        int seconds = obj % 60;
        sb.AppendFormat("{0:00}:{1:00}", minutes, seconds);
        matchDurationText.text = sb.ToString();
    }

    private void OnDestroy()
    {
        gameManager.OnGameTimerUpdated -= UpdateTimeDisplay;
        EventBus<GameStateChangeEvent>.Unregister(gameStateChangeEvent);
    }
}