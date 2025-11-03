using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Services.Core;
using UnityEngine;

public class ServerSingleton : MonoBehaviour
{
    private static ServerSingleton instance;
    public ServerGameManager GameManager { get; private set; }

    public static ServerSingleton Instance
    {
        get
        {
            if (instance == null)
            {
                instance = FindObjectOfType<ServerSingleton>();
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

    public async Task CreateServer(NetworkObject playerPrefab)
    {
        await UnityServices.InitializeAsync();

        GameManager = new ServerGameManager(
            ApplicationData.IP(),
            ApplicationData.Port(),
            ApplicationData.QPort(),
            NetworkManager.Singleton,
            playerPrefab
            );
    }

    private void OnDestroy()
    {
        GameManager?.Dispose();
    }
}
