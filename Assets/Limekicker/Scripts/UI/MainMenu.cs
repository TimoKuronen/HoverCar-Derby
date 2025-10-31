using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private TMP_Text queueStatusText;
    [SerializeField] private TMP_Text queueTimerText;
    [SerializeField] private TMP_Text findMatchButtonText;
    [SerializeField] private TMP_InputField joinCodeField;

    private bool isMatchmaking;
    private bool isCanceling;

    private void Start()
    {
        if (ClientSingleton.Instance == null)
            return;

        Cursor.SetCursor(null, Vector2.zero, CursorMode.Auto);

        queueStatusText.text = string.Empty;
        queueTimerText.text = string.Empty;
    }

    public async void FindMatchButtonPressed()
    {
        if (isCanceling)
            return;

        if (isMatchmaking)
        {
            queueStatusText.text = "Canceling...";
            isCanceling = true;
            // logic here to cancel matchmaking
            isCanceling = false;
            isMatchmaking = false;

            findMatchButtonText.text = "Find Match";
            queueStatusText.text = string.Empty;

            return;
        }

        // queue logic here
        findMatchButtonText.text = "Cancel";
        queueStatusText.text = "Searching...";
        isMatchmaking = true;
    }

    public async void StartHost()
    {
        await HostSingleton.Instance.GameManager.StartHostAsync();
    }

    public async void StartClient()
    {
        string joinCode = joinCodeField.text.ToUpper();

        await ClientSingleton.Instance.GameManager.StartClientAsync(joinCode);
    }
}
