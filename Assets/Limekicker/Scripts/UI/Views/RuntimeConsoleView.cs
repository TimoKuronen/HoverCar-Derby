using System.Text;
using TMPro;
using UnityEngine;

/// <summary>
/// Displays Unity log errors and exceptions. Attach to the same panel prefab as the diagnostic TMP field.
/// </summary>
public class RuntimeConsoleView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private int maxLines = 10;
    [SerializeField] private int maxCharsPerEntry = 200;
    [SerializeField] private bool includeWarnings;
    [SerializeField] private GameObject content;

    private readonly StringBuilder logBuilder = new StringBuilder();

    private void OnEnable()
    {
        if (logText != null)
            logText.text = string.Empty;

        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(string logString, string stackTrace, LogType type)
    {
        if (logText == null)
            return;

        if (type != LogType.Error && type != LogType.Exception && !(includeWarnings && type == LogType.Warning))
            return;

        content.SetActive(true);

        string entry = logString;
        if (type == LogType.Error || type == LogType.Exception)
        {
            string firstStackLine = string.Empty;
            if (!string.IsNullOrEmpty(stackTrace))
            {
                int newlineIndex = stackTrace.IndexOf('\n');
                firstStackLine = newlineIndex > 0 ? stackTrace[..newlineIndex] : stackTrace;
            }

            entry = $"{logString}\n{firstStackLine}".Trim();
        }

        if (entry.Length > maxCharsPerEntry)
            entry = entry[..maxCharsPerEntry] + "...";

        logBuilder.AppendLine(entry);

        string[] lines = logBuilder.ToString().Split('\n');
        if (lines.Length > maxLines)
        {
            logBuilder.Clear();
            for (int i = lines.Length - maxLines; i < lines.Length; i++)
            {
                if (!string.IsNullOrEmpty(lines[i]))
                    logBuilder.AppendLine(lines[i]);
            }
        }

        logText.text = logBuilder.ToString();
    }
}
