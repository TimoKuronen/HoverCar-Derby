using Unity.Netcode;
using UnityEngine;

/// <summary>
/// NetBootstrap entry: creates the host and client singletons, authenticates, and loads the main menu.
/// Scene flow is documented in docs/architecture.md.
/// </summary>
public class ApplicationController : MonoBehaviour
{
    [SerializeField] private ClientSingleton clientPrefab;
    [SerializeField] private HostSingleton hostPrefab;
    [SerializeField] private NetworkObject playerPrefab;

    private async void Start()
    {
        DontDestroyOnLoad(gameObject);

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
}
