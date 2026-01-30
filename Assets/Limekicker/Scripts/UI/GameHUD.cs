using System.Collections;
using TMPro;
using UnityEngine;

public class GameHUD : MonoBehaviour
{
    public static GameHUD Instance { get; private set; }
    public TextMeshProUGUI startCounterText;
    public TextMeshProUGUI GoText;
    public GameObject pauseMenu;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
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
