using System.Text;
using TMPro;
using UnityEngine;

public class NotificationConsoleView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI notificationText;
    [SerializeField] private int maxLines = 8;
    [SerializeField] private int maxCharsPerEntry = 160;

    private readonly StringBuilder buffer = new StringBuilder();
    private EventBinding<UserNotificationEvent> notificationBinding;

    private void OnEnable()
    {
        if (notificationText != null)
            notificationText.text = string.Empty;

        notificationBinding = new EventBinding<UserNotificationEvent>(HandleNotification);
        EventBus<UserNotificationEvent>.Register(notificationBinding);

        TryShowHostJoinCode();
    }

    private void TryShowHostJoinCode()
    {
        if (!NetworkSession.IsHostActive)
            return;

        string joinCode = NetworkSession.GetHostJoinCode();
        if (string.IsNullOrEmpty(joinCode))
            return;

        HandleNotification(new UserNotificationEvent
        {
            Message = $"Join code: {joinCode}",
            Severity = NotificationSeverity.Info
        });
    }

    private void OnDisable()
    {
        if (notificationBinding != null)
            EventBus<UserNotificationEvent>.Unregister(notificationBinding);
    }

    private void HandleNotification(UserNotificationEvent notificationEvent)
    {
        if (notificationText == null || string.IsNullOrEmpty(notificationEvent.Message))
            return;

        string entry = FormatEntry(notificationEvent.Message, notificationEvent.Severity);
        buffer.AppendLine(entry);
        TrimBuffer();
        notificationText.text = buffer.ToString();
    }

    private string FormatEntry(string message, NotificationSeverity severity)
    {
        string prefix = severity switch
        {
            NotificationSeverity.Warning => "[!]",
            NotificationSeverity.Error => "[X]",
            _ => "[i]"
        };

        string text = message;
        if (text.Length > maxCharsPerEntry)
            text = text[..maxCharsPerEntry] + "...";

        return $"{prefix} {text}";
    }

    private void TrimBuffer()
    {
        string[] lines = buffer.ToString().Split('\n');
        if (lines.Length <= maxLines)
            return;

        buffer.Clear();
        for (int i = lines.Length - maxLines; i < lines.Length; i++)
        {
            if (!string.IsNullOrEmpty(lines[i]))
                buffer.AppendLine(lines[i]);
        }
    }
}
