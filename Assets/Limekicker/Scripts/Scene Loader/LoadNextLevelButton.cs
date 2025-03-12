using UnityEngine;
using UnityEngine.UI;

public class LoadNextLevelButton : MonoBehaviour
{
    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(LoadNextScene);
    }

    void LoadNextScene()
    {
        if (Loader.GetCurrentScene() == Loader.Scene.TempMenu)
            StartCoroutine(Loader.CallDelayedLoad(Loader.Scene.timo_sandbox, 0.5f));
        else if (Loader.GetCurrentScene() == Loader.Scene.timo_sandbox)
            StartCoroutine(Loader.CallDelayedLoad(Loader.Scene.TempMenu, 0.5f));
    }

    private void OnDestroy()
    {
        GetComponent<Button>().onClick.RemoveAllListeners();
    }
}
