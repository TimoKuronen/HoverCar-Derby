using UnityEngine;

/// <summary>
/// Persists player sound and music enablement in PlayerPrefs.
/// </summary>
public static class AudioPreferences
{
    private const string SfxEnabledKey = "Audio_SfxEnabled";
    private const string MusicEnabledKey = "Audio_MusicEnabled";

    public static bool IsSfxEnabled()
    {
        return PlayerPrefs.GetInt(SfxEnabledKey, 1) == 1;
    }

    public static void SetSfxEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(SfxEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static bool IsMusicEnabled()
    {
        return PlayerPrefs.GetInt(MusicEnabledKey, 1) == 1;
    }

    public static void SetMusicEnabled(bool enabled)
    {
        PlayerPrefs.SetInt(MusicEnabledKey, enabled ? 1 : 0);
        PlayerPrefs.Save();
    }
}

