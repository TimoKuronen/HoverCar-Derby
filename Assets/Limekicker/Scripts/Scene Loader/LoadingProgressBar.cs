using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadingProgressBar : MonoBehaviour
{
    [SerializeField] private Image progressBar;
    [SerializeField] private TextMeshProUGUI textElement;

    private float timer;

    private void Update()
    {
        float progress = Loader.GetLoadingProgress();
        progressBar.fillAmount = Mathf.Clamp01(progress);

        timer += Time.deltaTime;
        if (timer > 0.5f)
        {
            timer = 0;
            UpdateText();
        }
    }

    private void UpdateText()
    {
        if (textElement.text == "Loading...")
        {
            textElement.text = "Loading";
        }
        else if (textElement.text == "Loading")
        {
            textElement.text = "Loading.";
        }
        else if (textElement.text == "Loading.")
        {
            textElement.text = "Loading..";
        }
        else if (textElement.text == "Loading..")
        {
            textElement.text = "Loading...";
        }
    }
}
