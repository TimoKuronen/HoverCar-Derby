using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using VContainer;

/// <summary>
/// Simple on‑screen debug console used during development.
///
/// Reads the host join code from <c>HostSingleton.Instance.GameManager</c> at
/// startup and appends arbitrary log lines to a TMP text field. Safe to remove
/// from scenes if you no longer need this debug UI; the rest of the networking
/// flow does not depend on it.
/// </summary>
public class InfoConsole : MonoBehaviour
{
    public static InfoConsole Instance;
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
        Instance = this;
        // NOTE: HostSingleton / GameManager may be null when not running as host.
        // Guard against that so this debug helper never breaks play mode.
        if (HostSingleton.Instance != null && HostSingleton.Instance.GameManager != null)
        {
            LogInfo(HostSingleton.Instance.GameManager.joinCode);
        }
    }

    public void LogInfo(string message)
    {
        logText.text += message + "\n";
    }
}
