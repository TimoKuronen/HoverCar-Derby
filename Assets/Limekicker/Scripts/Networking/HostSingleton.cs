using UnityEngine;

public class HostSingleton : MonoBehaviour
{
    private static HostSingleton instance;

    private HostGameManager gameManager;

    public static HostSingleton Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<HostSingleton>();
                if (instance == null)
                {
                    Debug.LogError("ClientSingleton instance not found in the scene.");
                }
            }
            return instance;
        }
    }
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    public void CreateHost()
    {
        gameManager = new HostGameManager();
    }
}
