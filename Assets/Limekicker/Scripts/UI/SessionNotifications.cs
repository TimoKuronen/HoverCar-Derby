using UnityEngine;

public static class SessionNotifications
{
    public static void Info(string message) => Raise(message, NotificationSeverity.Info);

    public static void Warn(string message, string logDetail = null)
    {
        string logMessage = string.IsNullOrWhiteSpace(logDetail) ? message : logDetail;
        if (!string.IsNullOrWhiteSpace(logMessage))
            Debug.LogWarning(logMessage);

        Raise(message, NotificationSeverity.Warning);
    }

    public static void Error(string message, string logDetail = null)
    {
        string logMessage = string.IsNullOrWhiteSpace(logDetail) ? message : logDetail;
        Debug.LogError(logMessage);
        Raise(message, NotificationSeverity.Error);
    }

    private static void Raise(string message, NotificationSeverity severity)
    {
        if (string.IsNullOrWhiteSpace(message))
            return;

        EventBus<UserNotificationEvent>.Raise(new UserNotificationEvent
        {
            Message = message.Trim(),
            Severity = severity
        });
    }
}
