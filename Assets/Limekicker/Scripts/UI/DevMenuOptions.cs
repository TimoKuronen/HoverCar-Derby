using UnityEngine;

public static class DevMenuOptions
{
    private const string SpawnBotKey = "SpawnBotForTesting";
    private const string SkipCountdownKey = "SkipCountdownForTesting";

    public static bool IsSpawnBotEnabled()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        return PlayerPrefs.GetInt(SpawnBotKey, 0) == 1;
#else
        return false;
#endif
    }

    public static bool IsSkipCountdownEnabled()
    {
#if UNITY_EDITOR || DEVELOPMENT_BUILD
        return PlayerPrefs.GetInt(SkipCountdownKey, 0) == 1;
#else
        return false;
#endif
    }

    public static void SetSpawnBotEnabled(bool value)
    {
        PlayerPrefs.SetInt(SpawnBotKey, value ? 1 : 0);
        PlayerPrefs.Save();
    }

    public static void SetSkipCountdownEnabled(bool value)
    {
        PlayerPrefs.SetInt(SkipCountdownKey, value ? 1 : 0);
        PlayerPrefs.Save();
    }
}
