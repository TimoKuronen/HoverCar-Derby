using System.Collections;
using TMPro;
using UnityEngine;

public class CountdownDisplay : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI countdownText;
    [SerializeField] private IntVariable countdownValue;

    private const float START_FONT_SIZE = 256f;
    private const float END_FONT_SIZE = 384f;
    private const float ANIMATION_DURATION = 0.75f;
    private const float GO_DISPLAY_DURATION = 1f;

    private Coroutine animationCoroutine;

    private void Start()
    {
        countdownValue.OnValueChanged += OnCountdownValueChanged;
        countdownText.gameObject.SetActive(false);
    }

    private void OnCountdownValueChanged(int value)
    {
        if (animationCoroutine != null)
        {
            StopCoroutine(animationCoroutine);
        }

        if (value < 0 || value > 3)
        {
            // Hide countdown for invalid values
            countdownText.gameObject.SetActive(false);
            return;
        }

        animationCoroutine = StartCoroutine(AnimateCountdown(value));
    }

    private IEnumerator AnimateCountdown(int countdownNumber)
    {
        countdownText.gameObject.SetActive(true);

        // Display "GO" when value is 0, otherwise display the number
        if (countdownNumber == 0)
        {
            countdownText.text = "GO";
            countdownText.fontSize = START_FONT_SIZE;

            // Animate GO with pulsing scale effect
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
        else
        {
            // Display number with font size animation
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
    }

    private void OnDestroy()
    {
        if (countdownValue != null)
        {
            countdownValue.OnValueChanged -= OnCountdownValueChanged;
        }
    }
}
