using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VContainer;

public class InfoConsole : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI logText;
    IPlayerSpawnManager playerSpawnManager;

    [Inject]    
    void Construct(IPlayerSpawnManager playerSpawnManager)
    {
        logText.text = string.Empty;
        this.playerSpawnManager = playerSpawnManager;
    }  

    public void LogInfo(string message)
    {
        logText.text += message + "\n";
    }
}
