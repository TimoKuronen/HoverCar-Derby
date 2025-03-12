using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ScreenFader : MonoBehaviour
{
    public static ScreenFader Instance;

    [SerializeField] private Image faderImage;

    public event Action<float> OnFadeOutStarted;
    public event Action OnFadeOutCompleted;

    public event Action<float> OnFadeInStarted;
    public event Action OnFadeInCompleted;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        Loader.OnSceneLoadStarted += InitiateFadeOut;
        SceneManager.sceneLoaded += CallFadeIn;

        Color color = faderImage.color;
        color.a = 1f;
        faderImage.color = color;
    }

    IEnumerator WaitForSceneToStart()
    {
        Debug.Log("waiting for scene to start because value is " + Services.Get<IGameManager>().GameSetupCompleted);
        yield return new WaitUntil(() => Services.Get<IGameManager>().GameSetupCompleted);
        Debug.Log("waiting done, lets move on");

       CallFadeIn(SceneManager.GetActiveScene());
    }

    private void CallFadeIn(Scene sceneToLoad, LoadSceneMode mode = default)
    {
        if (sceneToLoad.name != Loader.Scene.LoaderScene.ToString())
        {
            if (Loader.IsGameScene() && !Services.Get<IGameManager>().GameSetupCompleted)
                StartCoroutine(WaitForSceneToStart());
            else
                StartCoroutine(FadeIn(0.5f));
        }
    }

    public void InitiateFadeOut()
    {
        if (faderImage.color.a == 1)
        {
            Debug.Log("fade out not started as we are already at black opacity");
            return;
        }
        //Debug.Log("current " + Loader.GetCurrentScene().ToString());
        //Debug.Log("loaders name " + Loader.Scene.LoaderScene.ToString());
        if (Loader.GetCurrentScene() != Loader.Scene.LoaderScene)
            StartCoroutine(FadeOut(0.5f));
    }

    public void InitiateFadeIn()
    {
        StartCoroutine(FadeIn(0.5f));
    }

    private IEnumerator FadeOut(float duration)
    {
        Debug.Log("fade out started");
        OnFadeOutStarted?.Invoke(duration);

        float elapsedTime = 0f;
        Color color = faderImage.color;
        color.a = 0f;
        faderImage.color = color;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(0f, 1f, elapsedTime / duration);
            faderImage.color = color;

            yield return null;
        }

        color.a = 1f;
        faderImage.color = color;

        OnFadeOutCompleted?.Invoke();
    }

    private IEnumerator FadeIn(float duration)
    {
        Debug.Log("fade in started");
        OnFadeInStarted?.Invoke(duration);

        float elapsedTime = 0f;
        Color color = faderImage.color;
        color.a = 1f;
        faderImage.color = color;

        while (elapsedTime < duration)
        {
            elapsedTime += Time.deltaTime;
            color.a = Mathf.Lerp(1f, 0f, elapsedTime / duration);
            faderImage.color = color;

            yield return null;
        }

        color.a = 0f;
        faderImage.color = color;

        OnFadeInCompleted?.Invoke();
    }

    private void OnDestroy()
    {
        Loader.OnSceneLoadStarted -= InitiateFadeOut;
        //Loader.OnSceneLoadCompleted -= InitiateFadeIn;
    }
}
