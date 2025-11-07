using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Handles player name input in Bootstrap scene (build index 1).
/// Scene flow: Bootstrap (1) -> MainMenu (2) when user clicks Connect.
/// For dedicated servers (headless), auto-skips to next scene without UI.
/// NOTE: NetBootstrap (0) runs ApplicationController which creates host/client and goes directly to MainMenu,
/// so Bootstrap scene is only used when starting directly from it (e.g., in editor for testing).
/// </summary>
public class NameSelector : MonoBehaviour
{
    [SerializeField] private TMP_InputField nameField;
    [SerializeField] private Button connectButton;
    [SerializeField] private int minNameLength = 2;
    [SerializeField] private int maxNameLength = 12;

    public const string PlayerNameKey = "PlayerName";

    void Start()
    {
        // Dedicated server (headless) build: skip name input and go to next scene
        // This only applies when Bootstrap scene is loaded directly (not via NetBootstrap)
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            // For dedicated server, use a default name or skip name requirement
            if (string.IsNullOrEmpty(PlayerPrefs.GetString(PlayerNameKey, string.Empty)))
            {
                PlayerPrefs.SetString(PlayerNameKey, "Server");
            }
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
            return;
        }

        // Load saved name or empty string
        nameField.text = PlayerPrefs.GetString(PlayerNameKey, string.Empty);

        HandleNameChanged();
    }

    public void HandleNameChanged()
    {
        string playerName = nameField.text;
        bool isValid = playerName.Length >= minNameLength && playerName.Length <= maxNameLength;
        connectButton.interactable = isValid;
    }

    /// <summary>
    /// Called when Connect button is pressed. Saves name and loads MainMenu scene.
    /// </summary>
    public void Connect()
    {
        PlayerPrefs.SetString(PlayerNameKey, nameField.text);

        // Bootstrap (1) -> MainMenu (2)
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);
    }
}
