using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Editor and development builds: passes the host's Relay join code to other local instances through a
/// file in the OS temp folder, so a joining window does not need the code copied by hand.
/// Compiled to no-ops in release builds.
/// </summary>
public static class JoinCodeHandoff
{
#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private const string FileName = "HoverCarDerby_joincode.txt";
    private static readonly TimeSpan MaxAge = TimeSpan.FromMinutes(30);

    private static string FilePath => Path.Combine(Path.GetTempPath(), FileName);
#endif

    /// <summary>True when this instance is configured to join automatically once a code appears.</summary>
    public static bool AutoJoinEnabled => DevPlayerIdentity.HasTag(DevPlayerIdentity.ClientTag);

    /// <summary>Stores the code with a UTC timestamp so stale files from a crashed host are ignored.</summary>
    public static void Publish(string joinCode)
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        if (string.IsNullOrEmpty(joinCode))
            return;

        try
        {
            File.WriteAllText(FilePath, $"{joinCode}\n{DateTime.UtcNow.Ticks}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[JoinCodeHandoff] Could not publish join code: {e.Message}");
        }
#endif
    }

    public static void Clear()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        try
        {
            if (File.Exists(FilePath))
                File.Delete(FilePath);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[JoinCodeHandoff] Could not clear join code: {e.Message}");
        }
#endif
    }

    /// <summary>Reads the published code if it exists and is recent.</summary>
    public static bool TryRead(out string joinCode)
    {
        joinCode = null;
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        try
        {
            if (!File.Exists(FilePath))
                return false;

            string[] lines = File.ReadAllText(FilePath).Split('\n');
            if (lines.Length < 2 || !long.TryParse(lines[1], out long ticks))
                return false;

            if (DateTime.UtcNow - new DateTime(ticks, DateTimeKind.Utc) > MaxAge)
                return false;

            joinCode = lines[0].Trim();
            return joinCode.Length > 0;
        }
        catch (IOException)
        {
            return false;
        }
#else
        return false;
#endif
    }
}
