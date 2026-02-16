using System;
using System.Text;
using TMPro;
using UnityEngine;

public class MatchDurationDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI matchDurationText;
    [SerializeField] private IntVariable matchDurationLeft;

    private EventBinding<GameStateChangeEvent> gameStateChangeEvent;
    private static readonly StringBuilder sb = new();

    private void Start()
    {
        matchDurationLeft.OnValueChanged += UpdateTimeDisplay;
        UpdateTimeDisplay(matchDurationLeft.Value);
        matchDurationText.gameObject.SetActive(false);

        gameStateChangeEvent = new EventBinding<GameStateChangeEvent>(HandleGameStateChange);
        EventBus<GameStateChangeEvent>.Register(gameStateChangeEvent);
    }

    private void HandleGameStateChange(GameStateChangeEvent @event)
    {
        if (@event.NewState is PlayState || @event.NewState is CountdownState)
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
        int minutes = obj / 60;
        int seconds = obj % 60;
        sb.AppendFormat("{0:00}:{1:00}", minutes, seconds);
        matchDurationText.text = sb.ToString();
        sb.Clear();
    }

    private void OnDestroy()
    {
        matchDurationLeft.OnValueChanged -= UpdateTimeDisplay;
        EventBus<GameStateChangeEvent>.Unregister(gameStateChangeEvent);
    }
}