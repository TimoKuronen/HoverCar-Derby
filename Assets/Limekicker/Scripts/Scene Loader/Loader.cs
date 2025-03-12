using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class Loader
{
    public enum Scene
    {
        Empty,
        TempMenu,
        LoaderScene,
        timo_sandbox
    }

    public static event Action OnSceneLoadStarted;

    private static AsyncOperation loadingAsyncOperation;
    private static Scene targetScene = Scene.TempMenu;

    private static float delayBeforeLoading = 0f;

    public static bool IsGameScene()
    {
        return GetCurrentScene() == Scene.Empty || GetCurrentScene() > Scene.LoaderScene;
    }

    /// <summary>
    /// Load a scene asynchronously with a loading screen.
    /// </summary>
    public static void Load(Scene scene, float delay = 0f)
    {
        targetScene = scene;
        delayBeforeLoading = delay;
        SceneManager.LoadScene(Scene.LoaderScene.ToString());
    }

    public static IEnumerator CallDelayedLoad(Scene scene, float delay = 0f)
    {
        OnSceneLoadStarted?.Invoke();

        yield return new WaitForSeconds(delay);

        targetScene = scene;
        SceneManager.LoadScene(Scene.LoaderScene.ToString());
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
        Load(currentScene);
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

        if (delay > 0)
        {
            yield return new WaitForSeconds(delay);
        }

        loadingAsyncOperation = SceneManager.LoadSceneAsync(scene.ToString());

        while (!loadingAsyncOperation.isDone)
        {
            yield return null;
        }

        //OnSceneLoadCompleted?.Invoke();
        Debug.Log("Current Scene: " + SceneManager.GetActiveScene().name);
    }

    /// <summary>
    /// Get loading progress (0 to 1).
    /// </summary>
    public static float GetLoadingProgress()
    {
        return loadingAsyncOperation?.progress ?? 1f;
    }
}