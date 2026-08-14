using TMPro;
using Unity.Collections;
using UnityEngine;

/// <summary>
/// Updates local UI text when the player network name changes.
/// </summary>
public class PlayerNameDisplay : MonoBehaviour
{
    [SerializeField] private PlayerController playerController;
    [SerializeField] private TextMeshProUGUI playerNameText;

    private void Start()
    {
        HandlePlayerNameChange(string.Empty, playerController.PlayerName.Value);

        playerController.PlayerName.OnValueChanged += HandlePlayerNameChange;
    }

    private void HandlePlayerNameChange(FixedString32Bytes oldName, FixedString32Bytes newName)
    {
        playerNameText.text = newName.ToString();
    }

    private void OnDestroy()
    {
        if (playerController != null && playerController.PlayerName != null)
        {
            try
            {
                playerController.PlayerName.OnValueChanged -= HandlePlayerNameChange;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning($"[PlayerNameDisplay] Failed to unsubscribe from PlayerName (expected during shutdown): {e.Message}");
            }
        }
    }
}
