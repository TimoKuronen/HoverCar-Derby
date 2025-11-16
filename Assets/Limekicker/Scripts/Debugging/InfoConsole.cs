using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VContainer;

public class InfoConsole : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI logText;
    private IPlayerSpawnManager playerSpawnManager;

    [Inject]    
    void Construct(IPlayerSpawnManager playerSpawnManager)
    {
        logText.text = string.Empty;
        this.playerSpawnManager = playerSpawnManager;
    }

    private void Start()
    {
        LogInfo(HostSingleton.Instance.GameManager.joinCode);
    }

    public void LogInfo(string message)
    {
        logText.text += message + "\n";
    }
}
