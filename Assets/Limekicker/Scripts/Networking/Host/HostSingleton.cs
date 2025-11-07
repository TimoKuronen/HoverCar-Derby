using Unity.Netcode;
using UnityEngine;

public class HostSingleton : MonoBehaviour
{
    private static HostSingleton instance;

    public HostGameManager GameManager { get; private set; }

    public static HostSingleton Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<HostSingleton>();
                if (instance == null)
                {
                    Debug.LogError("HostSingleton instance not found in the scene.");
                }
            }
            return instance;
        }
    }
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Creates HostGameManager for client-hosted games.</summary>
    public void CreateHost(NetworkObject playerPrefab)
    {
        GameManager = new HostGameManager(playerPrefab);
    }

    private void OnDestroy()
    {
        GameManager?.Dispose();
    }
}
