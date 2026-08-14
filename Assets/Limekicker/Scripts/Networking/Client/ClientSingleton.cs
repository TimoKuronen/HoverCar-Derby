using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// Scene singleton that owns the client game manager lifecycle.
/// </summary>
public class ClientSingleton : MonoBehaviour
{
    private static ClientSingleton instance;

    public ClientGameManager GameManager { get; private set; }

    public static ClientSingleton Instance
    {
        get
        {
                if (instance == null)
                {
                    instance = FindObjectOfType<ClientSingleton>();
                }
            return instance;
        }
    }
    void Start()
    {
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>Creates ClientGameManager and initializes authentication. Returns true if successful.</summary>
    public async Task<bool> CreateClient()
    {
        GameManager = new ClientGameManager();

        return await GameManager.InitAsync();
    }

    void OnDestroy()
    {
        GameManager?.Dispose();
    }
}