using System.Collections;
using TMPro;
using UnityEngine;

public class CountdownDisplayView : MonoBehaviour, ICountdownDisplayView
{
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private IntVariable countdownValue;

    private const float START_FONT_SIZE = 256f;
    private const float END_FONT_SIZE = 384f;
    private const float ANIMATION_DURATION = 0.75f;
    private const float GO_DISPLAY_DURATION = 1f;

    private Coroutine animationCoroutine;
    private CountdownPresenter presenter;

    private void Start()
    {
        presenter = new CountdownPresenter(this, countdownValue);
        presenter.Initialize();
        Hide();
    }

    public void ShowCountdown(int number)
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(AnimateCountdown(number));
    }

    public void ShowGo()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        animationCoroutine = StartCoroutine(AnimateGo());
    }

    public void Hide()
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }
        countdownText.gameObject.SetActive(false);
    }

    private IEnumerator AnimateCountdown(int countdownNumber)
    {
        countdownText.gameObject.SetActive(true);
        countdownText.text = countdownNumber.ToString();
        countdownText.transform.localScale = Vector3.one;
        countdownText.fontSize = START_FONT_SIZE;

        float elapsedTime = 0f;
        while (elapsedTime < ANIMATION_DURATION)
        {
            elapsedTime += Time.deltaTime;
            float lerpValue = elapsedTime / ANIMATION_DURATION;
            countdownText.fontSize = Mathf.Lerp(START_FONT_SIZE, END_FONT_SIZE, lerpValue);
            yield return null;
        }

        countdownText.fontSize = END_FONT_SIZE;
    }

    private IEnumerator AnimateGo()
    {
        countdownText.gameObject.SetActive(true);
        countdownText.text = "GO";
        countdownText.fontSize = START_FONT_SIZE;

        float timer = 0f;
        while (timer < GO_DISPLAY_DURATION)
        {
            timer += Time.deltaTime;
            float scale = 1f + 0.5f * Mathf.Sin(timer * 5f);
            countdownText.transform.localScale = Vector3.one * scale;
            yield return null;
        }

        countdownText.gameObject.SetActive(false);
        countdownText.transform.localScale = Vector3.one;
    }

    private void OnDestroy()
    {
        presenter?.Dispose();
    }
}
