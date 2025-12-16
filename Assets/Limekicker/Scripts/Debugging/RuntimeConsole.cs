using System.Text;
using TMPro;
using UnityEngine;

public class RuntimeConsole : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private int maxLines = 15;
    [SerializeField] private int maxCharsPerEntry = 200; // limit per log entry

    private StringBuilder logBuilder = new StringBuilder();

    private void OnEnable()
    {
        logText.text = string.Empty;
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        // Only show errors and exceptions
        if (type != LogType.Error && type != LogType.Exception)
            return;

        // Take only the first line of the stack trace (usually the file & line number)
        string firstStackLine = "";
        if (!string.IsNullOrEmpty(stackTrace))
        {
            int newlineIndex = stackTrace.IndexOf('\n');
            firstStackLine = newlineIndex > 0 ? stackTrace[..newlineIndex] : stackTrace;
        }

        // Combine log + first stack trace line
        string entry = $"{logString}\n{firstStackLine}".Trim();

        // Limit entry length so it doesn’t flood the UI
        if (entry.Length > maxCharsPerEntry)
            entry = entry[..maxCharsPerEntry] + "...";

        logBuilder.AppendLine(entry);

        // Keep only recent lines
        string[] lines = logBuilder.ToString().Split('\n');
        if (lines.Length > maxLines)
        {
            logBuilder.Clear();
            for (int i = lines.Length - maxLines; i < lines.Length; i++)
                logBuilder.AppendLine(lines[i]);
        }

        logText.text = logBuilder.ToString();
    }
}
