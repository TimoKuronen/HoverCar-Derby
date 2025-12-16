using TMPro;
using Unity.Collections;
using UnityEngine;

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
        playerController.PlayerName.OnValueChanged -= HandlePlayerNameChange;
    }
}
