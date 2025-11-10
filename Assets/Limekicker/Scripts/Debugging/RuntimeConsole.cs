using UnityEngine;
using UnityEngine.UI;
using System.Text;
using TMPro;

public class RuntimeConsole : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI logText;
    [SerializeField] private int maxLines = 20;

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
        string prefix = "";
        if (type == LogType.Error || type == LogType.Exception)
        {
            //prefix = "<color=red>[ERROR]</color> ";
            //else if (type == LogType.Warning)
            //    prefix = "<color=yellow>[WARN]</color> ";
            //else
            //    prefix = "<color=gray>[INFO]</color> ";

            logBuilder.AppendLine(prefix + logString);

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
}