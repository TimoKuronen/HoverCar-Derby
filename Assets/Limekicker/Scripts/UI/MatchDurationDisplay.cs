using System.Text;
using TMPro;
using UnityEngine;
using VContainer;

public class MatchDurationDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI matchDurationText;

    private IGameManager gameManager;

    [Inject]
    public void Construct(IGameManager gameManager)
    {
        this.gameManager = gameManager;
        this.gameManager.OnGameTimerUpdated += UpdateTimeDisplay;
        
        UpdateTimeDisplay(gameManager.Context.roundDurationInSeconds);
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
    }
}