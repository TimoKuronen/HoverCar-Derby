using System.Collections;
using System.Text;
using TMPro;
using UnityEngine;

internal class CountdownState : IGameState
{
    private readonly GameManager gameManager;
    private TextMeshProUGUI countdownText;
    private readonly StringBuilder stringBuilder = new StringBuilder(4);
    
    private const float START_FONT_SIZE = 256f;
    private const float END_FONT_SIZE = 384f;
    private const float ANIMATION_DURATION = 0.75f;
    private const float POST_GO_DELAY = 0.5f;

    public CountdownState(GameManager manager)
    {
        gameManager = manager;
    }

    public void Enter()
    {
        if (GameHUD.Instance != null)
        {
            countdownText = GameHUD.Instance.startCounterText;
        }

        CoroutineMonoBehavior.Instance.StartCoroutine(CountdownCoroutine());
    }

    public void Exit() 
    {
        stringBuilder.Clear();
    }

    public void Update() { }

    private IEnumerator CountdownCoroutine()
    {
        if (countdownText == null)
        {
            Debug.LogError("CountdownState: countdownText is null. Cannot display countdown.");
            gameManager.ChangeState(new PlayState());
            yield break;
        }

        gameManager.Context.raceCamera.Priority = 20;
        countdownText.gameObject.SetActive(true);

        yield return AnimateCountdown(3);
        yield return AnimateCountdown(2);
        yield return AnimateCountdown(1);
        
        yield return GameHUD.Instance.AnimateGoText();
        RaiseCountdownEvent("GO", 0);

        gameManager.ChangeState(new PlayState());

        yield return new WaitForSeconds(POST_GO_DELAY);
        countdownText.gameObject.SetActive(false);
    }

    private IEnumerator AnimateCountdown(int countdownNumber)
    {
        if (countdownText == null)
            yield break;

        stringBuilder.Clear();
        stringBuilder.Append(countdownNumber);
        string countdownString = stringBuilder.ToString();

        countdownText.text = countdownString;
        RaiseCountdownEvent(countdownString, countdownNumber);

        float elapsedTime = 0f;
        countdownText.fontSize = START_FONT_SIZE;

        while (elapsedTime < ANIMATION_DURATION)
        {
            elapsedTime += Time.deltaTime;
            float lerpValue = elapsedTime / ANIMATION_DURATION;
            countdownText.fontSize = Mathf.Lerp(START_FONT_SIZE, END_FONT_SIZE, lerpValue);
            yield return null;
        }

        countdownText.fontSize = END_FONT_SIZE;
    }

    private void RaiseCountdownEvent(string countdownValue, int countdownNumber)
    {
        EventBus<CountdownEvent>.Raise(new CountdownEvent
        {
            CountdownValue = countdownValue,
            CountdownNumber = countdownNumber
        });
    }
}