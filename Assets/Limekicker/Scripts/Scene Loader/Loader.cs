using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Loader
{
    public enum Scene
    {
        Empty,
        MainMenu,
        Loader,
        PlayScene
    }

    public static event Action OnSceneLoadStarted;

    private static AsyncOperation loadingAsyncOperation;
    private static Scene targetScene = Scene.MainMenu;

    private static float delayBeforeLoading = 0f;

    public static IEnumerator CallDelayedLoad(Scene scene, float delay = 0f)
    {
        OnSceneLoadStarted?.Invoke();

        yield return new WaitForSecondsRealtime(delay);

        targetScene = scene;
        Debug.Log("Loading scene: " + scene);
        SceneManager.LoadScene(Scene.Loader.ToString());
    }

    /// <summary>
    /// Call the loader callback when the loading scene is fully loaded.
    /// </summary>
    public static void LoaderCallback()
    {
        GameObject loadingGameObject = new GameObject("Loading Game Object");
        loadingGameObject.AddComponent<CoroutineMonoBehavior>().StartCoroutine(LoadSceneAsync(targetScene, delayBeforeLoading));
    }

    /// <summary>
    /// Reload the current scene.
    /// </summary>
    public static void Restart()
    {
        Scene currentScene = GetCurrentScene();
        CallDelayedLoad(currentScene, 1);
    }

    /// <summary>
    /// Get the currently active scene as an enum.
    /// </summary>
    public static Scene GetCurrentScene()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (Enum.TryParse(sceneName, out Scene sceneEnum))
        {
            return sceneEnum;
        }

        return default;
    }

    private static IEnumerator LoadSceneAsync(Scene scene, float delay)
    {
        OnSceneLoadStarted?.Invoke();

        Debug.Log("Begin loading scene: " + scene);

        if (delay > 0)
        {
            yield return new WaitForSecondsRealtime(delay);
        }

        loadingAsyncOperation = SceneManager.LoadSceneAsync(scene.ToString());

        while (!loadingAsyncOperation.isDone)
        {
            yield return null;
        }

        Reset();
        Debug.Log("Current Scene: " + SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Get loading progress (0 to 1).
    /// </summary>
    public static float GetLoadingProgress()
    {
        return loadingAsyncOperation?.progress ?? 1f;
    }

    public static void Reset()
    {
        loadingAsyncOperation = null;
        targetScene = Scene.MainMenu;
        delayBeforeLoading = 0f;
    }

    public static bool IsGameScene()
    {
        return GetCurrentScene() != Scene.Empty && GetCurrentScene() > Scene.Loader;
    }
}