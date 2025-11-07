using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Entry point for NetBootstrap scene (build index 0).
/// Handles initialization based on build type:
/// - Dedicated Server: Creates ServerSingleton, loads PlayScene, starts server with matchmaking
/// - Client/Host: Creates HostSingleton + ClientSingleton, authenticates, goes to MainMenu
/// 
/// SCENE FLOW:
/// 0. NetBootstrap (this) -> ApplicationController runs immediately
///    - If dedicated server: Load PlayScene, start server
///    - If client/host: Authenticate -> MainMenu (2)
/// 1. Bootstrap -> NameSelector (only if started directly, not via NetBootstrap)
/// 2. MainMenu -> User chooses: Host, Join via code, Matchmake, or Browse lobbies
/// 3. PlayScene -> Gameplay (loaded by host/server when game starts)
/// 
/// NOTE: When starting from NetBootstrap, Bootstrap scene is SKIPPED.
/// Player name should be set via PlayerPrefs before NetBootstrap runs, or use default.
/// </summary>
public class ApplicationController : MonoBehaviour
{
    [SerializeField] private ClientSingleton clientPrefab;
    [SerializeField] private HostSingleton hostPrefab;
    [SerializeField] private ServerSingleton serverPrefab;
    [SerializeField] private NetworkObject playerPrefab;

    private const string GameSceneName = "PlayScene";
    private const string MenuSceneName = "MainMenu";

    private async void Start()
    {
        DontDestroyOnLoad(gameObject);

        // Check if running as dedicated server (headless, no graphics)
        await LaunchInMode(SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null);
    }

    /// <summary>
    /// Launches in either dedicated server mode or client/host mode.
    /// </summary>
    private async Task LaunchInMode(bool isDedicatedServer)
    {
        if (isDedicatedServer)
        {
            // DEDICATED SERVER PATH (Linux build with UNITY_SERVER define)
            // Used for matchmaking: Multiplay allocates server, matchmaker assigns players
            Application.targetFrameRate = 60;

            ServerSingleton serverSingleton = Instantiate(serverPrefab);

            // Load game scene, then initialize server with matchmaking
            StartCoroutine(LoadGameSceneAsync(serverSingleton));
        }
        else
        {
            // CLIENT/HOST PATH (Editor, Windows, Mobile builds)
            // Creates both host and client singletons for host mode
            // Authenticates user, then goes to MainMenu
            
            // Ensure player name is set (fallback if Bootstrap was skipped)
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
                // Go to MainMenu (scene index 2)
                clientSingleton.GameManager.GoToMenu();
            }
        }
    }

    /// <summary>
    /// Loads PlayScene for dedicated server, then initializes server with matchmaking.
    /// Server waits for Multiplay allocation, gets matchmaker payload, starts backfilling.
    /// </summary>
    private IEnumerator LoadGameSceneAsync(ServerSingleton serverSingleton)
    {
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(GameSceneName);

        while (!asyncOperation.isDone)
        {
            yield return null;
        }

        // Initialize server (Unity Services, NetworkManager setup)
        Task createServerTask = serverSingleton.CreateServer(playerPrefab);
        
        yield return new WaitUntil(() => createServerTask.IsCompleted);

        // Start server: wait for Multiplay allocation, get matchmaker payload, start backfilling
        Task startServerTask = serverSingleton.GameManager.StartServerAsync();

        yield return new WaitUntil(() => startServerTask.IsCompleted);
    }
}
