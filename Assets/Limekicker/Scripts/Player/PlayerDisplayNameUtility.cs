using Unity.Collections;

/// <summary>
/// Helpers for default join-order player display names.
/// </summary>
public static class PlayerDisplayNameUtility
{
    public static bool ShouldUseJoinOrderName(string userName)
    {
        if (string.IsNullOrWhiteSpace(userName))
            return true;

        return userName == "Player"
            || userName == "!!Missing Name!!"
            || userName == "Unknown"
            || userName == "Server";
    }

    public static FixedString32Bytes BuildJoinOrderName(int joinOrderOneBased)
    {
        return new FixedString32Bytes($"Player {joinOrderOneBased}");
    }
}
