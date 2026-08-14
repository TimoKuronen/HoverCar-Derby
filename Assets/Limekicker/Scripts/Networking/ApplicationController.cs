using System.Collections;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// NetBootstrap entry: dedicated-server setup or client/host singleton bootstrap.
/// Scene flow is documented in docs/architecture.md.
/// </summary>
public class ApplicationController : MonoBehaviour
{
    [SerializeField] private ClientSingleton clientPrefab;
    [SerializeField] private HostSingleton hostPrefab;
    [SerializeField] private ServerSingleton serverPrefab;
    [SerializeField] private NetworkObject playerPrefab;

    private const string GameSceneName = "PlayScene";

    private async void Start()
    {
        DontDestroyOnLoad(gameObject);
        await LaunchInMode(SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null);
    }

    private async Task LaunchInMode(bool isDedicatedServer)
    {
        if (isDedicatedServer)
        {
            Application.targetFrameRate = 60;
            ServerSingleton serverSingleton = Instantiate(serverPrefab);
            StartCoroutine(LoadGameSceneAsync(serverSingleton));
            return;
        }

        if (string.IsNullOrEmpty(PlayerPrefs.GetString(NameSelector.PlayerNameKey, string.Empty)))
        {
            PlayerPrefs.SetString(NameSelector.PlayerNameKey, "Player");
            Debug.LogWarning("[ApplicationController] Player name not set, using default 'Player'. Consider starting from Bootstrap scene or setting name before NetBootstrap.");
        }

        HostSingleton hostSingleton = Instantiate(hostPrefab);
        hostSingleton.CreateHost(playerPrefab);

        ClientSingleton clientSingleton = Instantiate(clientPrefab);
        bool authenticated = await clientSingleton.CreateClient();

        if (authenticated)
        {
            clientSingleton.GameManager.GoToMenu();
        }
    }

    private IEnumerator LoadGameSceneAsync(ServerSingleton serverSingleton)
    {
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(GameSceneName);

        while (!asyncOperation.isDone)
        {
            yield return null;
        }

        Task createServerTask = serverSingleton.CreateServer(playerPrefab);
        yield return new WaitUntil(() => createServerTask.IsCompleted);

        Task startServerTask = serverSingleton.GameManager.StartServerAsync();
        yield return new WaitUntil(() => startServerTask.IsCompleted);
    }
}
