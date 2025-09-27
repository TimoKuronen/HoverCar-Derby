using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

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