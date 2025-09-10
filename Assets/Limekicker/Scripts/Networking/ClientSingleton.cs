using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

public class ClientSingleton : MonoBehaviour
{
    private static ClientSingleton instance;

    private ClientGameManager gameManager;

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

    public async Task CreateClient()
    {
        gameManager = new ClientGameManager();

        await gameManager.InitAsync();
    }
}